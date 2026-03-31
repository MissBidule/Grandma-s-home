using UnityEngine;

/*
 * @brief Interface handled universally by PredictiveMovement to compute Client-Side Prediction movement steps.
 * @details Every playable entity implements this to resolve their specific physical and logical behaviors based on predictive inputs.
 */
public interface ISimulateMovement
{
    /*
     * @brief Resolves input actions during one physics step
     * @param _d Structural data encompassing all network-synchronized inputs
     * @return void
     */
    public void SimulateMovement(PredictiveInputData _d);
}
