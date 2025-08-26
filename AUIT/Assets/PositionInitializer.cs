using UnityEngine;


public class PositionInitializer : MonoBehaviour
{
    public GameObject DropPoint;
    // public GameObject DropPointSemiStationary;
    // public GameObject DropPointMoving;

    private ScenarioType scenarioType;

    public float _yThresholdForRespawn = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.y < _yThresholdForRespawn)
        {
            Respawn();
        }
    }

    // public void Initialize(ScenarioType scenarioType)
    // {
    //     this.scenarioType = scenarioType;
    //     Respawn();
    // }

    public void Respawn()
    {
        // switch (scenarioType)
        // {
        //     case ScenarioType.Stationary:
        //         break;
        //     case ScenarioType.SemiStationary:
        //         transform.parent = DropPointSemiStationary.transform;
        //         break;
        //     case ScenarioType.Moving:
        //         transform.parent = DropPointMoving.transform;
        //         break;
        //     default:
        //         Debug.LogError("Invalid scenario type for respawn.");
        //         return;
        // }

        // Reset the position, rotation, and scale of the GameObject
        transform.position = DropPoint.transform.position;
        transform.localRotation = DropPoint.transform.rotation;
    }
}
