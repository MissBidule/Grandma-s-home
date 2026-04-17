# ?? Guide de Test des Corrections de Lobby

## ?? Objectif
Vérifier que les 8 bugs ont été correctement corrigés et que le comportement du lobby est maintenant stable sur toutes les configurations d'ordinateur.

---

## ?? Tests à Effectuer

### TEST 1 : Vérification de la Compilation ?
**Status** : Déjà effectué dans le workspace

```
? Build successful - Pas d'erreurs
? 8 files modifiés
? Tous les usings ajoutés
```

---

### TEST 2 : Test de Mémoire (Memory Leak)

**Objectif** : Vérifier que les événements sont correctement désabonnés

**Steps** :
1. Ouvrir le lobby 10 fois
2. Quitter le lobby 10 fois
3. Ouvrir le Profiler Unity (Window > Analysis > Profiler)
4. Vérifier l'onglet "Memory"

**Résultat Attendu** :
- ? La mémoire devrait **croître légèrement** puis **se stabiliser**
- ? Pas de croissance continue du heap
- ? Les objets devraient être garbage-collectés

**Résultat Mauvais** :
- ? Mémoire qui croît indéfiniment
- ? Heap qui ne diminue jamais

**Commande Debug** :
```csharp
// Dans LobbyManager pour vérifier les subscriptions
Debug.Log($"Event subscriptions count: {_onLobbyUpdatedHandler1 != null ? 1 : 0}");
```

---

### TEST 3 : Test de Performance (BUG #2)

**Objectif** : Vérifier que le lobby n'a pas de lag quand plusieurs joueurs joignent

**Steps** :
1. Créer un lobby avec 4 joueurs
2. Ouvrir le Profiler (Window > Analysis > Profiler)
3. Observer le FPS et CPU usage

**Résultat Attendu** :
- ? FPS reste > 60 FPS
- ? CPU usage < 5% pour le lobby
- ? Pas de spike perceptible

**Résultat Mauvais** :
- ? FPS chute à < 30
- ? CPU spike > 50%
- ? Lag visible lors du join

**Logs à Vérifier** :
```
"Updating player count: 1"
"Updating player count: 2"
"Updating player count: 3"
"Updating player count: 4"
```

---

### TEST 4 : Test de Reconnexion (BUG #3)

**Objectif** : Vérifier que host et client se reconnectent au même moment

**Steps** :
1. Créer un lobby (host)
2. Joindre avec client
3. Recharger la scène (tous deux)
4. Vérifier la reconnexion

**Résultat Attendu** :
- ? Les deux se reconnectent environ en même temps
- ? Logs montrent "Reconnecting as host..." et "Reconnecting as client..."
- ? Pas de timeout

**Résultat Mauvais** :
- ? Client attend 0.5s supplémentaire
- ? Comportement différent host vs client
- ? Timeout réseau

**Logs à Vérifier** :
```
"Starting lobby reconnect..."
"Reconnecting as host/client..."
"Lobby marked as started"
```

---

### TEST 5 : Test d'Authentification (BUG #4)

**Objectif** : Vérifier que l'authentification gère les erreurs gracefully

**Steps** :
1. Joindre un lobby
2. Vérifier les logs d'authentification
3. Tester sur PC où RoleKeeper charge tard

**Résultat Attendu** :
- ? Authentification réussit
- ? Pas de NullReferenceException
- ? Logs clairs si erreur

**Résultat Mauvais** :
- ? Crash avec NullReferenceException
- ? Message d'erreur vague

**Logs à Vérifier** :
```
"RoleKeeper found" (bon)
"RoleKeeper not found - using UNKNOWN" (acceptable)
"Error in authentication" (problème)
```

---

### TEST 6 : Test de Timing Réseau (BUG #5)

**Objectif** : Vérifier que le timing de connexion s'adapte à la PC

**Simulation PC Lent** :
```csharp
// Ajouter dans Start de ConnectionStarter pour test
yield return new WaitForSeconds(2f); // Simuler serveur lent
```

**Steps** :
1. Créer lobby sur PC "lent" (ou ajouter delay)
2. Vérifier que le client attend
3. Vérifier pas de timeout

**Résultat Attendu** :
- ? Client s'adapte à la vitesse du serveur
- ? Pas de timeout même si serveur lent
- ? Pas de delay inutile si serveur rapide

**Résultat Mauvais** :
- ? Timeout après 1 seconde
- ? Delay 1s inutile sur PC rapide

**Logs à Vérifier** :
```
"Waiting for server to be ready..."
"Server is ready, starting client..."
"Starting client connection..."
```

---

### TEST 7 : Test UI (BUG #7)

**Objectif** : Vérifier que l'UI handle les structures différentes

**Steps** :
1. Modifier la structure UI du lobby
2. Vérifier que pas de crash
3. Observer les logs

**Résultat Attendu** :
- ? Pas de NullReferenceException
- ? Logs indiquent quel composant manque
- ? UI partiellement mis à jour

**Résultat Mauvais** :
- ? Crash avec NullRef
- ? Pas de message d'erreur

**Logs à Vérifier** :
```
"m_serverName is null" (structure invalide)
"No TextMeshProUGUI found" (structure invalide)
"Cannot update lobby screen: lobby is invalid" (normal)
```

---

### TEST 8 : Test Exception Handling (BUG #8)

**Objectif** : Vérifier que les exceptions async sont catchées

**Steps** :
1. Créer un scénario où SetLobbyStartedAsync échoue (simuler)
2. Vérifier que l'erreur est loggée
3. Vérifier que l'utilisateur est notifié

**Résultat Attendu** :
- ? Exception loggée avec message clair
- ? OnError event déclenché
- ? Pas d'exception non gérée

**Résultat Mauvais** :
- ? Exception silencieuse
- ? Pas de notification utilisateur

**Logs à Vérifier** :
```
"Lobby marked as started" (succès)
"Failed to mark lobby as started: ..." (erreur gérée)
```

---

## ?? Tests Complets (Multi-Configurations)

### Configuration 1 : PC Lent
- CPU : i5 de 2015
- RAM : 8GB
- Réseau : WiFi (100ms latence)

**Tests** :
- [ ] Lobby se charge sans lag
- [ ] Pas de timeout réseau
- [ ] Memory stable

### Configuration 2 : PC Rapide
- CPU : i9 de 2023
- RAM : 32GB
- Réseau : Ethernet (10ms latence)

**Tests** :
- [ ] Lobby chargement instantané
- [ ] Pas de delay inutile
- [ ] Memory minimal

### Configuration 3 : Mauvais Réseau
- Simuler latence haute (500ms)
- Simuler packet loss (5%)
- Simuler connexion instable

**Tests** :
- [ ] Pas de timeout prématuré
- [ ] Reconnexion fonctionne
- [ ] Erreurs loggées

---

## ?? Checklist de Validation

### Code Quality
- [ ] Build sans erreurs
- [ ] Pas de warnings
- [ ] Code formaté correctement
- [ ] Comments en place

### Performance
- [ ] FPS > 60 lors du lobby
- [ ] CPU < 5% pour lobby
- [ ] Memory stable (pas de leak)
- [ ] Network latency acceptable

### Stability
- [ ] Pas de crash NullRef
- [ ] Pas d'exception non gérée
- [ ] Events correctement désabonnés
- [ ] Reconnexion fonctionne

### User Experience
- [ ] Erreurs claires loggées
- [ ] UI ne glitch pas
- [ ] Timing naturel
- [ ] Pas de comportement étrange

---

## ?? Debugging Tips

### Si Bug #1 persiste (Memory Leak)
```csharp
// Dans UnsubscribeFromProviderEvents, ajouter:
Debug.Log($"Unsubscribing {nameof(_onLobbyUpdatedHandler1)}");
Debug.Log($"Handlers before: {_currentProvider.OnLobbyUpdated.GetInvocationList().Length}");
```

### Si Bug #2 persiste (Performance)
```csharp
// Dans HandleNewMembers:
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... boucle
stopwatch.Stop();
PurrLogger.Log($"HandleNewMembers took {stopwatch.ElapsedMilliseconds}ms", this);
```

### Si Bug #3 persiste (Race Condition)
```csharp
// Dans ReconnectCoroutine:
PurrLogger.Log($"Reconnect started at frame {Time.frameCount}", this);
// ... puis plus tard
PurrLogger.Log($"Reconnect completed at frame {Time.frameCount}", this);
```

### Si Bug #5 persiste (Timeout)
```csharp
// Dans StartClient:
PurrLogger.Log($"Server ready: {_networkManager.isActiveAndEnabled}", this);
PurrLogger.Log($"Elapsed time: {Time.time - startTime}s", this);
```

---

## ?? Support

Si vous trouvez encore des problèmes :

1. **Vérifier les logs** : Chercher "ERROR" ou "Warning"
2. **Vérifier la build** : S'assurer que tous les fichiers sont compilés
3. **Vérifier le setup** : S'assurer que le provider est correctement assigné
4. **Tester sur plusieurs PC** : Pour confirmer que c'est pas machine-specific

---

## ? Validation Finale

Une fois tous les tests passés :

```
? BUG #1 : Memory leak éliminé
? BUG #2 : Performance améliorée
? BUG #3 : Race condition résolue
? BUG #4 : Authentification sûre
? BUG #5 : Timing adaptatif
? BUG #6 : Événements dédoublés
? BUG #7 : UI validée
? BUG #8 : Exceptions gérées

? Lobby est maintenant STABLE sur toutes les configurations!
```
