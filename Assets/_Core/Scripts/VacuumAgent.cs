using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class VacuumAgent : Agent
{
    public Transform target;
    public Transform badTarget;
    public float speed = 0.1f;
    public float testRange = 8f;
    public float maxTime = 10;

    private float _timer;
    private bool _isRunningEpisode;
    private Vector2 _targetWaypoint;
    private Vector2 _badTargetWaypoint;
    private Vector2 _velocity;

    private void Start()
    {
        _timer = 0;
    }

    private void Update()
    {
        if (!_isRunningEpisode) return;

        _timer += Time.deltaTime;
        if (_timer > maxTime)
        {
            _timer = 0;
            EndingEpisode();
        }

        UpdateTarget();
        UpdateBadTarget();
        UpdateSelf();
    }

    private void UpdateTarget()
    {
        Vector2 pos = target.localPosition;
        Vector2 vector = _targetWaypoint - pos;
        target.localPosition = pos + vector.normalized * (speed * 0.8f) * Time.deltaTime;

        float dist = Vector2.Distance(_targetWaypoint, target.localPosition);
        if (dist < 0.2f)
        {
            _targetWaypoint = GetRandomVector2();
        }
    }

    private void UpdateBadTarget()
    {
        Vector2 pos = badTarget.localPosition;
        Vector2 vector = _badTargetWaypoint - pos;
        badTarget.localPosition = pos + vector.normalized * (speed * 0.8f) * Time.deltaTime;

        float dist = Vector2.Distance(_badTargetWaypoint, badTarget.localPosition);
        if (dist < 0.2f)
        {
            _badTargetWaypoint = GetRandomVector2();
        }
    }

    private void UpdateSelf()
    {
        transform.localPosition += (Vector3) _velocity * Time.deltaTime;

        float distanceToTarget = Vector3.Distance(transform.localPosition, target.localPosition);
        float distanceToBadTarget = Vector3.Distance(transform.localPosition, badTarget.localPosition);

        if (distanceToTarget < 0.4f)
        {
            SetReward(maxTime);
            EndingEpisode();
        }

        if (distanceToBadTarget < 0.4f)
        {
            SetReward(-maxTime);
            EndingEpisode();
        }

        if (Vector3.Distance(transform.localPosition, Vector3.zero) > testRange)
        {
            EndingEpisode();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.parent.position, testRange);
    }

    private void EndingEpisode()
    {
        _isRunningEpisode = false;
        EndEpisode();
    }

    private Vector2 GetRandomVector2()
    {
        return (Vector2) Random.onUnitSphere * testRange / 2;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = GetRandomVector2();
        TargetSetup(target);
        TargetSetup(badTarget);

        _targetWaypoint = GetRandomVector2();
        _badTargetWaypoint = GetRandomVector2();

        _timer = 0;
        _isRunningEpisode = true;
    }

    private void TargetSetup(Transform targetTfm)
    {
        targetTfm.localPosition = GetRandomVector2();
        while ((transform.localPosition - targetTfm.localPosition).magnitude < 1)
        {
            targetTfm.localPosition = GetRandomVector2();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation((Vector2) transform.localPosition);
        sensor.AddObservation((Vector2) target.localPosition);
        sensor.AddObservation((Vector2) badTarget.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Vector2 controlSignal = Vector2.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.y = actionBuffers.ContinuousActions[1];
        if (controlSignal.magnitude > 1.0f)
        {
            controlSignal = controlSignal.normalized;
        }

        _velocity = controlSignal * speed;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxisRaw("Horizontal");
        continuousActionsOut[1] = Input.GetAxisRaw("Vertical");
    }
}