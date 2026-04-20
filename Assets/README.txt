-- Il faut donner les MeshRenderer (en faisant un drag & drop des objets dans Hierarchy vers le champs Renderers To Modify dans le script PlaydoughShaderManager ou JellyGhostShaderManager (selon si vous voulez des objets en pâte à modele rou en gélatine fantôme, respectivement) dans Inspector) pour qu'ils aient l'effet respectif avec la couleur que vous avez donné sur Blender par exemple sans devoir la repréciser à chaque fois comme avant

-- Utilisez ou regardez les prefabs PlaydoughShaderManager et JellyGhostShaderManager pour voir les valeurs à avoir/valeurs de base

-- Il y a une infobulle (Tooltip) à chaque champs des deux scripts au hover de la souris pour aider

-- Le bouton Update Renderers To Modify n'est pas nécessaire; le changement se fait lors de l'ajout d'un élément dans la liste des MeshRenders plus haut, mais il est quand même présent au cas où (on n'est jamais exempt de situations particulières non couvertes)