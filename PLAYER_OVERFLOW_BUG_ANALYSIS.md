# ?? Analyse: Player Overflow Bug dans le Lobby

## ?? Problème Identifié

Apparemment, y a des soucis **"d'overflow" sur les players quand les clients s'updataient**.

## ?? Root Causes Potentielles

### BUG #1 : Réutilisation de FindAnyObjectByType dans MemberEntry
**Fichier** : `Assets/Lobby/Runtime/Misc/UI/MemberEntry.cs`

```csharp
public void Init(LobbyUser _user)
{
    _roleKeeper = FindAnyObjectByType<RoleKeeper>();  // ?? INIT
}

public void SetRole(bool isGhost, int skin)
{
    FindAnyObjectByType<RoleKeeper>().SwitchRole(...);  // ?? APPELÉ À CHAQUE UPDATE !
    avatar.texture = _roleKeeper.GetSkinImage(_memberId);
}
```

**Problème** :
- `FindAnyObjectByType` appelé **à chaque fois que le rôle change**
- Mais `_roleKeeper` est déjà en cache - **inconsistance !**
- Peut causer des appels multiples à `SwitchRole()`

### BUG #2 : Double Instantiation des MemberEntry
**Fichier** : `Assets/Lobby/Runtime/Misc/UI/LobbyMemberList.cs`

```csharp
private async void HandleNewMembers(Lobby room)
{
    var existingMembers = content.GetComponentsInChildren<MemberEntry>();

    foreach (var member in room.Members)
    {
        if (Array.Exists(existingMembers, x => x.MemberId == member.Id))
            continue;  // ?? Devrait skip si déjà existe

        var entry = Instantiate(memberEntryPrefab, content);  // CRÉER NOUVELLE ENTRY
    }
}
```

**Problème** :
- ? Check d'existence fait avec `GetComponentsInChildren()`
- ?? Mais `Array.Exists()` peut être **inaccurate si content enfants pas à jour**
- Résultat : **Overflow de MemberEntry dupliquées**

### BUG #3 : Pas de Layout Group Contrôle
**Symptôme Probable** : "overflow" = **sortir des bounds du container**

Possible causes :
- Pas de **LayoutGroup** (VerticalLayoutGroup, GridLayoutGroup) pour organiser les entries
- Pas de **preferredHeight** configuré
- **ContentSizeFitter** pas en place pour redimensionner

### BUG #4 : Membres Supprimés Pas Complètement Nettoyés
**Fichier** : `Assets/Lobby/Runtime/Misc/UI/LobbyMemberList.cs`

```csharp
private void HandleLeftMembers(Lobby room)
{
    var childrenToRemove = new List<Transform>();

    for (int i = 0; i < content.childCount; i++)
    {
        var child = content.GetChild(i);
        if (!child.TryGetComponent(out MemberEntry member))
            continue;

        if (!room.Members.Exists(x => x.Id == member.MemberId))
        {
            m_roleKeeper.RemoveRole(member.MemberId);
            childrenToRemove.Add(child);  // ?? LIST MAIS PAS IMMEDIATE DESTROY
        }
    }

    foreach (var child in childrenToRemove)
    {
        Destroy(child.gameObject);  // ?? DESTROY PEUT PRENDRE 1-2 FRAMES
    }
}
```

**Problème** :
- `Destroy()` n'est pas immédiat - le GameObject reste en mémoire 1-2 frames
- Pendant ce temps, `GetComponentsInChildren()` peut toujours le voir
- **Race condition possible**

## ?? Overflow Scenario

```
Frame 1:
  Player A joins ? MemberEntry créée
  
Frame 2:
  Lobby update arrive
  HandleExistingMembers() check members
  HandleNewMembers() check "Player A existe déjà?"
  ?? GetComponentsInChildren() peut ou pas voir la nouvelle entry
  
Frame 3:
  Player A join update re-arrive
  HandleNewMembers() créer DEUXIÈME entry pour Player A!
  ? OVERFLOW (2 entries pour 1 joueur)
  
Frame 4:
  Destroy() enfin exécuté ? mais 2ème entry toujours là
```

## ?? Solutions

### Solution 1 : Dictionary Cache pour MemberEntries

```csharp
public class LobbyMemberList : MonoBehaviour
{
    // ? Cache membres par ID
    private Dictionary<string, MemberEntry> m_memberEntries = 
        new Dictionary<string, MemberEntry>();

    public void LobbyDataUpdate(Lobby room)
    {
        HandleExistingMembers(room);
        HandleNewMembers(room);
        HandleLeftMembers(room);
        HandleInGameLock(room);
    }

    private async void HandleNewMembers(Lobby room)
    {
        foreach (var member in room.Members)
        {
            // ? Check dans DICTIONARY au lieu de GetComponentsInChildren
            if (m_memberEntries.ContainsKey(member.Id))
                continue;  // Entry existe déjà

            var entry = Instantiate(memberEntryPrefab, content);
            entry._lobbyManager = m_lobbyManager;
            entry._ownId = await entry._lobbyManager.GetPlayer();
            entry.Init(member);
            
            // ? AJOUTER AU CACHE
            m_memberEntries[member.Id] = entry;
            
            m_roleKeeper.AddRole(member.Id, member.DisplayName, 
                                member.IsGhost, member.Skin, entry._ownId == member.Id);
            entry.SetRole(member.IsGhost, member.Skin);
            if (entry.SetHost()) HandleHostOptions(entry, room);
        }
    }

    private void HandleLeftMembers(Lobby room)
    {
        // ? Itérer sur les CLÉS du cache
        var membersToRemove = new List<string>();
        foreach (var memberId in m_memberEntries.Keys)
        {
            if (!room.Members.Exists(x => x.Id == memberId))
            {
                membersToRemove.Add(memberId);
            }
        }

        foreach (var memberId in membersToRemove)
        {
            if (m_memberEntries.TryGetValue(memberId, out var entry))
            {
                m_roleKeeper.RemoveRole(memberId);
                // ? IMMEDIATE DESTROY + DEFERRED (plus robuste)
                Destroy(entry.gameObject);
                m_memberEntries.Remove(memberId);
            }
        }
    }

    public void OnLobbyLeave()
    {
        m_roleKeeper.DeleteList();
        m_memberEntries.Clear();  // ? Clear cache
        foreach (Transform child in content)
            Destroy(child.gameObject);
    }
}
```

### Solution 2 : Cohérence SetRole dans MemberEntry

```csharp
public void SetRole(bool isGhost, int skin)
{
    _isGhost = isGhost;
    _skin = skin;
    
    // ? Utiliser _roleKeeper en cache au lieu de FindAnyObjectByType
    if (_roleKeeper == null)
        _roleKeeper = FindAnyObjectByType<RoleKeeper>();
    
    _roleKeeper.SwitchRole(_memberId, isGhost, skin);
    avatar.texture = _roleKeeper.GetSkinImage(_memberId);
}
```

### Solution 3 : Layout Group pour UI

Ajouter dans le prefab ou dans le code:

```csharp
// Dans LobbyMemberList.Start()
VerticalLayoutGroup vlg = content.gameObject.GetComponent<VerticalLayoutGroup>();
if (vlg == null)
{
    vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
    vlg.childForceExpandHeight = false;
    vlg.childForceExpandWidth = false;
    vlg.spacing = 5f;
}

ContentSizeFitter csf = content.gameObject.GetComponent<ContentSizeFitter>();
if (csf == null)
{
    csf = content.gameObject.AddComponent<ContentSizeFitter>();
    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
}
```

### Solution 4 : Vraiment Immédiate Destroy

```csharp
// DANGEROUS mais parfois nécessaire:
DestroyImmediate(entry.gameObject);  // Pas recommendé mais élimine le delay
m_memberEntries.Remove(memberId);

// Ou mieux: use UnityProxy si disponible
if (UnityProxy != null)
    UnityProxy.DestroyDirectly(entry.gameObject);
```

## ?? Test pour Vérifier le Fix

```csharp
// Dans LobbyMemberList pour debug
#if UNITY_EDITOR
[ContextMenu("Debug: Check for Duplicates")]
private void DebugCheckDuplicates()
{
    var entries = content.GetComponentsInChildren<MemberEntry>();
    var idCounts = new Dictionary<string, int>();
    
    foreach (var entry in entries)
    {
        if (!idCounts.ContainsKey(entry.MemberId))
            idCounts[entry.MemberId] = 0;
        idCounts[entry.MemberId]++;
    }
    
    foreach (var kvp in idCounts)
    {
        if (kvp.Value > 1)
        {
            Debug.LogError($"DUPLICATE MEMBER: {kvp.Key} appears {kvp.Value} times!");
        }
    }
    
    Debug.Log($"Total entries: {entries.Length}, Unique members: {idCounts.Count}");
}
#endif
```

## ?? Checklist Fix

- [ ] Implémenter Dictionary cache pour MemberEntries
- [ ] Cohérence SetRole avec _roleKeeper cache
- [ ] Ajouter LayoutGroup + ContentSizeFitter
- [ ] Tester avec 4+ joueurs
- [ ] Vérifier pas de duplicates avec debug menu
- [ ] Vérifier UI ne sort pas des bounds
- [ ] Valider sur PC lent et rapide
