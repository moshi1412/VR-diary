using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MagLevHover : MonoBehaviour
{
    [Header("Base Binding")]
    public Transform baseTransform;
    public bool autoFindBase = true;
    public float baseSearchInterval = 0.5f;
    private float _nextSearchTime = 0f;

    [Header("Hover Settings")]
    public float hoverHeight = 1.0f;
    public float kP = 200f;
    public float kD = 30f;
    public float maxUpForce = 1500f;

    [Header("Lateral Stabilization")]
    public float kLat = 60f;
    public float kLatD = 8f;
    public float maxLatForce = 1000f;

    [Header("Orientation (Optional)")]
    public float alignTorque = 10f;
    public float angularDamping = 1.5f;

    [Header("Force Region (Cylindrical, aligned with baseTransform.up)")]
    public bool useForceRegion = true;
    public float regionRadius = 1.2f;
    public float regionHeightMin = 0.0f;
    public float regionHeightMax = 2.5f;
    [Range(0f, 0.9f)]
    public float edgeSmooth = 0.3f;

    [Header("Glow On Enter Region")]
    public Color glowColor = Color.cyan;
    public float glowOnIntensity = 5f;
    public float glowOffIntensity = 0f;
    [Range(0f, 1f)]
    public float glowThreshold = 0.05f;
    public float glowLerpSpeed = 8f;

    [Header("Auto Reactivation")]
    public bool autoReactivate = true;   // �Զ�����/������������
    public float reactivateDelay = 0.5f; // �ӳټ�⣬��ֹ��Ե����

    [Header("Gizmos")]
    public float gizmoRadius = 0.15f;
    public bool drawRegion = true;

    // Internals
    private Rigidbody rb;
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private float _currentGlow = 0f;
    private bool hoverActive = true;         // ��ǰ�����Ƿ���Ч
    private float nextReactivateCheck = 0f;  // �´μ��ʱ��

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _renderer = GetComponent<Renderer>();
        if (_renderer == null) _renderer = GetComponentInChildren<Renderer>();
        if (_renderer != null) _mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        if (autoFindBase && baseTransform == null) TryResolveNearestBase();
    }

    public void SetBase(Transform t) => baseTransform = t;

    void FixedUpdate()
    {
        if (baseTransform == null && autoFindBase && Time.time >= _nextSearchTime)
        {
            _nextSearchTime = Time.time + baseSearchInterval;
            TryResolveNearestBase();
        }
        if (baseTransform == null) return;

        Vector3 n = baseTransform.up.normalized;
        Vector3 toBall = transform.position - baseTransform.position;
        float height = Vector3.Dot(toBall, n);
        Vector3 lateral = Vector3.ProjectOnPlane(toBall, n);
        float radial = lateral.magnitude;

        // Compute influence
        float influence = 1f;
        if (useForceRegion)
        {
            influence *= SmoothBand01(radial, regionRadius, edgeSmooth);
            influence *= SmoothBand01Below(height, regionHeightMin, edgeSmooth);
            influence *= SmoothBand01Above(height, regionHeightMax, edgeSmooth);
        }

        // Update glow regardless of state
        UpdateGlow(influence);

        // --- Auto reactivate logic ---
        if (autoReactivate)
        {
            if (hoverActive && influence <= 0f)
            {
                hoverActive = false;
                nextReactivateCheck = Time.time + reactivateDelay;
            }
            else if (!hoverActive && influence > 0.2f && Time.time >= nextReactivateCheck)
            {
                hoverActive = true;
            }
        }

        // --- If not active, do minimal damping only ---
        if (!hoverActive)
        {
            rb.AddTorque(-rb.angularVelocity * angularDamping, ForceMode.Force);
            return;
        }

        // --- Normal hover behaviour ---
        float vVert = Vector3.Dot(rb.linearVelocity, n);
        float error = hoverHeight - height;

        // Gravity compensation (mass-invariant hover)
        float gravityComp = rb.mass * Physics.gravity.magnitude;

        float fUpScalar = kP * error - kD * vVert + gravityComp;
        fUpScalar = Mathf.Clamp(fUpScalar, -maxUpForce, maxUpForce);
        Vector3 fUp = n * fUpScalar;

        // Lateral stabilization
        Vector3 vLat = Vector3.ProjectOnPlane(rb.linearVelocity, n);
        Vector3 fLat = (-kLat * lateral) + (-kLatD * vLat);
        if (fLat.magnitude > maxLatForce) fLat = fLat.normalized * maxLatForce;

        Vector3 totalForce = (fUp + fLat) * influence;
        rb.AddForce(totalForce, ForceMode.Force);

        // Orientation alignment
        Vector3 axis = Vector3.Cross(transform.up, n);
        float angleMag = axis.magnitude;
        if (angleMag > 1e-4f)
        {
            Vector3 torque = axis.normalized * (alignTorque * angleMag) - rb.angularVelocity * angularDamping;
            rb.AddTorque(torque, ForceMode.Force);
        }
    }

    // Auto-find nearest base
    void TryResolveNearestBase()
    {
        MagLevBase best = null;
        float bestDistSqr = float.PositiveInfinity;
        if (MagLevBase.Instances.Count == 0) return;
        Vector3 p = transform.position;
        foreach (var b in MagLevBase.Instances)
        {
            if (b == null) continue;
            float d = (b.transform.position - p).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = b;
            }
        }
        if (best != null) baseTransform = best.transform;
    }

    // Emission control
    void UpdateGlow(float influence)
    {
        if (_renderer == null || _mpb == null) return;
        float targetGlow = 0f;
        if (influence > glowThreshold)
        {
            float t = Mathf.InverseLerp(glowThreshold, 1f, influence);
            targetGlow = t * t * (3f - 2f * t);
        }
        _currentGlow = Mathf.MoveTowards(_currentGlow, targetGlow, glowLerpSpeed * Time.fixedDeltaTime);
        _renderer.GetPropertyBlock(_mpb);
        Color emission = glowColor * Mathf.Lerp(glowOffIntensity, glowOnIntensity, _currentGlow);
        _mpb.SetColor("_EmissionColor", emission);
        var mat = _renderer.sharedMaterial;
        if (mat != null) mat.EnableKeyword("_EMISSION");
        _renderer.SetPropertyBlock(_mpb);
    }

    // Smooth helpers
    float SmoothBand01(float r, float R, float edge)
    {
        if (edge <= 0f) return r <= R ? 1f : 0f;
        float inner = R * (1f - edge);
        if (r <= inner) return 1f;
        if (r >= R) return 0f;
        float t = Mathf.InverseLerp(R, inner, r);
        return t * t * (3f - 2f * t);
    }
    float SmoothBand01Below(float h, float Hmin, float edge)
    {
        if (edge <= 0f) return h >= Hmin ? 1f : 0f;
        float total = Mathf.Max(0.0001f, regionHeightMax - regionHeightMin);
        float low = Hmin - total * edge;
        if (h <= low) return 0f;
        if (h >= Hmin) return 1f;
        float t = Mathf.InverseLerp(low, Hmin, h);
        return t * t * (3f - 2f * t);
    }
    float SmoothBand01Above(float h, float Hmax, float edge)
    {
        if (edge <= 0f) return h <= Hmax ? 1f : 0f;
        float total = Mathf.Max(0.0001f, regionHeightMax - regionHeightMin);
        float high = Hmax + total * edge;
        if (h >= high) return 0f;
        if (h <= Hmax) return 1f;
        float t = Mathf.InverseLerp(high, Hmax, h);
        return t * t * (3f - 2f * t);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!baseTransform || !drawRegion) return;
        Gizmos.color = Color.cyan;
        Vector3 n = baseTransform.up.normalized;
        Vector3 targetPos = baseTransform.position + n * hoverHeight;
        Gizmos.DrawWireSphere(targetPos, gizmoRadius);
        if (useForceRegion)
        {
            Gizmos.color = new Color(0f, 0.6f, 1f, 0.5f);
            Vector3 origin = baseTransform.position;
            float h0 = regionHeightMin;
            float h1 = regionHeightMax;
            Vector3 n0 = n;
            Vector3 t1 = Vector3.Cross(n0, Vector3.right);
            if (t1.sqrMagnitude < 1e-4f) t1 = Vector3.Cross(n0, Vector3.forward);
            t1.Normalize();
            Vector3 t2 = Vector3.Cross(n0, t1);
            int seg = 36;
            DrawCircle(origin + n0 * h0, t1, t2, regionRadius, seg);
            DrawCircle(origin + n0 * h1, t1, t2, regionRadius, seg);
            Gizmos.DrawLine(origin + n0 * h0 + t1 * regionRadius, origin + n0 * h1 + t1 * regionRadius);
            Gizmos.DrawLine(origin + n0 * h0 - t1 * regionRadius, origin + n0 * h1 - t1 * regionRadius);
            Gizmos.DrawLine(origin + n0 * h0 + t2 * regionRadius, origin + n0 * h1 + t2 * regionRadius);
            Gizmos.DrawLine(origin + n0 * h0 - t2 * regionRadius, origin + n0 * h1 - t2 * regionRadius);
        }
    }
    void DrawCircle(Vector3 center, Vector3 xAxis, Vector3 yAxis, float r, int seg)
    {
        Vector3 prev = center + xAxis * r;
        for (int i = 1; i <= seg; i++)
        {
            float ang = (i / (float)seg) * Mathf.PI * 2f;
            Vector3 p = center + (Mathf.Cos(ang) * xAxis + Mathf.Sin(ang) * yAxis) * r;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
#endif
}
