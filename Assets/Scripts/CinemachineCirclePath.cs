using UnityEngine;
using Cinemachine;
using Waypoint = Cinemachine.CinemachineSmoothPath.Waypoint;

public class CinemachineCirclePath : MonoBehaviour
{
    CinemachineSmoothPath path;

    [Range(2, 128)]
    public int points = 16;
    [Range(1f, 1000f)]
    public float radius = 5f;

    private Waypoint[] m_Waypoints = new Waypoint[0];

    private void OnValidate()
    {
        if (!path) path = this.GetComponent<CinemachineSmoothPath>();
        if (!path) return;

        Apply();
    }

    public void Apply()
    {
        m_Waypoints = new Waypoint[points];

        float angle = 0;
        
        for (int i = 0; i < points; i++)
        {
            m_Waypoints[i].position = new Vector3(
                Mathf.Sin(Mathf.Deg2Rad * angle) * (radius * 0.5f),
                0,
                Mathf.Cos(Mathf.Deg2Rad * angle) * (radius * 0.5f));

            angle += (360 / (float)points);
        }

        path.m_Waypoints = m_Waypoints;
        path.InvalidateDistanceCache();
    }
}