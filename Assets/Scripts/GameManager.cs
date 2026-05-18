using Unity.MLAgents;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Phases")]
    public Phase phase0;
    public Phase phase1;

    private Phase currentPhase;

    private void Start()
    {
        DeterminePhase();
    }

    private void FixedUpdate()
    {
        if (currentPhase != null)
            currentPhase.UpdatePhase();
    }

    private void DeterminePhase()
    {
        float phaseParam = (int)
            Academy.Instance.EnvironmentParameters.GetWithDefault("player_phase", 0.0f);

        switch ((int)phaseParam)
        {
            case 0:
                SwitchPhase(phase0);
                break;
            case 1:
                SwitchPhase(phase1);
                break;
        }
    }

    public void SwitchPhase(Phase newPhase)
    {
        if (newPhase == null)
            return;

        if (phase0 != null)
            phase0.gameObject.SetActive(false);
        if (phase1 != null)
            phase1.gameObject.SetActive(false);

        currentPhase = newPhase;

        currentPhase.gameObject.SetActive(true);

        currentPhase.InitPhase();
    }
}
