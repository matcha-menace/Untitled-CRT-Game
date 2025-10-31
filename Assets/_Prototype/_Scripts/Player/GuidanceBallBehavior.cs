using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class GuidanceBallBehavior : MonoBehaviour
{
    [SerializeField] private Transform guidanceBallTransform;
    [SerializeField] private LayerMask guidanceBallBlockMask;
    [SerializeField] private LayerMask playerMask;
    [SerializeField] private Color defaultColor;
    private Transform t;
    private Vector3 guidanceDir;
    private float defaultDist;
    private float guidanceDist;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private float defaultHeight;
    private Vector3 targetPos;

    void Start()
    {
        t = transform;
        defaultHeight = guidanceBallTransform.position.y;
        defaultDist = (guidanceBallTransform.position - t.localPosition).magnitude;
    }
    
    void Update()
    {
        guidanceDir = (t.forward * 1f + new Vector3(0, guidanceBallTransform.position.y, 0)).normalized;
        RaycastHit hit;
        if (Physics.SphereCast(t.localPosition, .2f, guidanceDir, out hit, defaultDist,
                guidanceBallBlockMask))
        {
            guidanceDist = hit.distance;
        }
        else guidanceDist = defaultDist;

        targetPos = t.localPosition + guidanceDir * guidanceDist;
        
        CameraDetection();
    }
    
    private void CameraDetection()
    {
        // Raycast from camera to ball to see if blocked
        var cam = Camera.main;
        var adjustedHeightPos = new Vector3(targetPos.x, defaultHeight, targetPos.z);
        if (cam != null && Physics.Raycast(cam.transform.position,
                (adjustedHeightPos - cam.transform.position).normalized,
                out RaycastHit hit, Vector3.Distance(cam.transform.position, adjustedHeightPos), guidanceBallBlockMask))
        {
            targetPos = adjustedHeightPos + Vector3.up * 1.5f;
        }
        else
        {
            targetPos = new Vector3(targetPos.x, defaultHeight, targetPos.z);
        }
        
        guidanceBallTransform.position = Vector3.Lerp(guidanceBallTransform.position,
            targetPos, Time.deltaTime * 5f);
    }
    
    public void SetBallColor(Color newColor)
    {
        var mat = guidanceBallTransform.GetComponent<Renderer>().material;
        mat.color = newColor;
        mat.SetColor(EmissionColor, newColor * 2);
        guidanceBallTransform.GetComponent<Light>().color = newColor;
    }
    
    public void ResetBallColor()
    {
        SetBallColor(defaultColor);
    }
}
