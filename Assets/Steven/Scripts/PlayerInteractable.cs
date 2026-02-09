using UnityEngine;

/**
@brief       Interface d'interaction côté joueur
@details     Permet au joueur de sélectionner un interactible (le plus proche) et d'interagir
*/
public interface PlayerInteractable
{
    /**
    @brief      Teste si le joueur peut interagir
    @param      _playerType: type du joueur (Child/Ghost)
    @return     true si interactible
    */
    bool CanInteract(PlayerType _playerType);

    /**
    @brief      Texte d'invite (prompt)
    @param      _playerType: type du joueur
    @return     texte
    */
    string GetPrompt(PlayerType _playerType);

    /**
    @brief      Appelé quand l'objet devient la cible sélectionnée
    @param      _playerType: type du joueur
    @return     void
    */
    void OnFocus(PlayerType _playerType);

    /**
    @brief      Appelé quand l'objet n'est plus la cible sélectionnée
    @param      _playerType: type du joueur
    @return     void
    */
    void OnUnfocus(PlayerType _playerType);

    /**
    @brief      Interaction principale (touche E)
    @param      _playerTransform: transform du joueur
    @param      _playerType: type du joueur
    @return     void
    */
    void Interact(Transform _playerTransform, PlayerType _playerType);
}
