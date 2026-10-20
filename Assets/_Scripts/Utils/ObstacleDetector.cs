using System.Collections.Generic;
using UnityEngine;

public class ObstacleDetector : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private Transform target;
    private HashSet<FadeObstacle> _currentFadeObstacles = new HashSet<FadeObstacle>();
    private HashSet<FadeObstacle> _hitThisFrame = new HashSet<FadeObstacle>();

    private void Start()
    {
        // Each host/client runs its own detector, so it only tracks the player it owns
        if (Player.LocalInstance != null)
            target = Player.LocalInstance.transform;
        else
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
    }

    private void OnDestroy()
    {
        Player.OnAnyPlayerSpawned -= Player_OnAnyPlayerSpawned;
    }

    private void Player_OnAnyPlayerSpawned(object sender, System.EventArgs e)
    {
        if (Player.LocalInstance != null)
            target = Player.LocalInstance.transform;
    }

    // Red = nothing hit, yellow = hit something without FadeObstacle, green = hit a FadeObstacle
    private void OnDrawGizmos()
    {
        if (target == null || cameraTransform == null)
            return;

        Vector3 dir = target.position - cameraTransform.position;
        if (Physics.Raycast(cameraTransform.position, dir.normalized, out RaycastHit hit, dir.magnitude))
        {
            Color color = hit.collider.GetComponent<FadeObstacle>() != null ? Color.green : Color.yellow;
            GizmosUtils.DrawRay(cameraTransform.position, dir, hit.distance, color);
            GizmosUtils.DrawWireSphere(hit.point, 0.2f, color);
        }
        else
        {
            GizmosUtils.DrawRay(cameraTransform.position, dir, dir.magnitude, Color.red);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Cast from the camera towards the target; RaycastAll keeps going through every collider in between.
        Vector3 dir = target.position - cameraTransform.position;
        _hitThisFrame.Clear();
        foreach (RaycastHit hit in Physics.RaycastAll(cameraTransform.position, dir.normalized, dir.magnitude))
        {
            FadeObstacle fadeObstacle = hit.collider.GetComponent<FadeObstacle>();
            if (fadeObstacle != null)
                _hitThisFrame.Add(fadeObstacle);
        }

        // Restore obstacles that no longer block (null check: obstacle may have been destroyed)
        foreach (FadeObstacle fadeObstacle in _currentFadeObstacles)
            if (fadeObstacle != null && !_hitThisFrame.Contains(fadeObstacle))
                fadeObstacle.StartFade(fadeObstacle.GetOpaqueAlpha());

        // Fade only the newly blocking ones so ongoing fades aren't restarted every frame
        foreach (FadeObstacle fadeObstacle in _hitThisFrame)
            if (!_currentFadeObstacles.Contains(fadeObstacle))
                fadeObstacle.StartFade(fadeObstacle.GetTransparentAlpha());

        // Swap sets instead of allocating new ones each frame
        (_currentFadeObstacles, _hitThisFrame) = (_hitThisFrame, _currentFadeObstacles);
    }
}

