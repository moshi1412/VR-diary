using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseMagLevController : MonoBehaviour
{
    [Header("Ball Detection")]
    public string ballTag = "Ball";
    public float detectionRadius = 3f;

    [Header("Hover Settings")]
    public float hoverHeight = 1.0f;
    public float kP = 200f;
    public float kD = 30f;
    public float kLat = 60f;
    public float kLatD = 8f;
    public float maxUpForce = 1500f;
    public float maxLatForce = 1000f;

    [Header("Force Region")]
    public bool useForceRegion = true;
    public float regionRadius = 1.2f;
    public float regionHeightMin = 0.0f;
    public float regionHeightMax = 2.5f;
    [Range(0f, 0.9f)]
    public float edgeSmooth = 0.3f;

    [Header("Glow Settings")]
    public Color glowColor = Color.cyan;
    public float glowOnIntensity = 5f;
    public float glowOffIntensity = 0f;
    [Range(0f, 1f)]
    public float glowThreshold = 0.05f;
    public float glowLerpSpeed = 8f;

    [Header("Launch Sequence")]
    public float prepHeightOffset = -0.5f;
    public float prepDuration = 1.0f;
    public float prepKPMult = 0.4f;
    public float prepKDMult = 0.6f;
    public float prepKLatMult = 1.5f;
    public float centerPosThreshold = 0.05f;
    public float centerHeightThreshold = 0.05f;
    public float centerSpeedThreshold = 0.2f;
    public float centerTimeout = 3.0f;

    public float launchImpulse = 6f;
    public float launchBoostKPMult = 2f;
    public float launchBoostKDMult = 1.5f;
    public float launchBoostTime = 0.5f;

    public readonly List<Rigidbody> balls = new List<Rigidbody>();

    // Glow 缓存（每个球对应一个 MaterialPropertyBlock）
    private readonly Dictionary<Renderer, MaterialPropertyBlock> glowBlocks =
        new Dictionary<Renderer, MaterialPropertyBlock>();

    Coroutine _launchRoutine;

    void FixedUpdate()
    {
        ScanBalls();

        foreach (var rb in balls)
        {
            float influence = ComputeInfluence(rb);
            ApplyGlow(rb, influence);
            if (influence > 0f)
                ApplyHover(rb, influence);
        }
    }

    // -------------------------------------------------------------------
    // 给按钮调用：全体球起飞
    // -------------------------------------------------------------------
    public void LaunchAll()
    {
        if (_launchRoutine != null)
            StopCoroutine(_launchRoutine);

        _launchRoutine = StartCoroutine(LaunchSequence());
    }

    // -------------------------------------------------------------------
    // 球扫描
    // -------------------------------------------------------------------
    void ScanBalls()
    {
        balls.Clear();
        float radius = Mathf.Max(detectionRadius, regionRadius);

        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var h in hits)
        {
            if (!h.CompareTag(ballTag)) continue;
            if (h.attachedRigidbody == null) continue;

            if (!balls.Contains(h.attachedRigidbody))
                balls.Add(h.attachedRigidbody);
        }
    }

    // -------------------------------------------------------------------
    // 力场 Influence 0..1
    // -------------------------------------------------------------------
    float ComputeInfluence(Rigidbody rb)
    {
        if (!useForceRegion) return 1f;

        Vector3 up = transform.up;
        Vector3 toBall = rb.position - transform.position;

        float h = Vector3.Dot(toBall, up);
        float radial = Vector3.ProjectOnPlane(toBall, up).magnitude;

        float t = 1f;
        t *= SmoothBand01(radial, regionRadius, edgeSmooth);
        t *= SmoothBand01Below(h, regionHeightMin, edgeSmooth);
        t *= SmoothBand01Above(h, regionHeightMax, edgeSmooth);

        return t;
    }

    bool IsInsideCoreRegion(Vector3 worldPos)
    {
        if (!useForceRegion) return true;

        Vector3 up = transform.up;
        Vector3 toBall = worldPos - transform.position;

        float h = Vector3.Dot(toBall, up);
        float radial = Vector3.ProjectOnPlane(toBall, up).magnitude;

        return radial <= regionRadius &&
               h >= regionHeightMin &&
               h <= regionHeightMax;
    }

    // -------------------------------------------------------------------
    // Glow
    // -------------------------------------------------------------------
    void ApplyGlow(Rigidbody rb, float inflFromForce)
    {
        Renderer r = rb.GetComponentInChildren<Renderer>();
        if (r == null) return;

        if (!glowBlocks.TryGetValue(r, out var mpb))
        {
            mpb = new MaterialPropertyBlock();
            glowBlocks[r] = mpb;
        }

        // --- 先做一个“硬边界”判断 ---
        bool insideCore = IsInsideCoreRegion(rb.position);
        float glowInfluence = insideCore ? inflFromForce : 0f;

        // 下面跟原来的逻辑一样，只是用 glowInfluence 来算发光强度
        float targetGlow = 0f;
        if (glowInfluence > glowThreshold)
        {
            float x = Mathf.InverseLerp(glowThreshold, 1f, glowInfluence);
            targetGlow = x * x * (3 - 2 * x);  // smoothstep
        }

        r.GetPropertyBlock(mpb);
        float current = mpb.GetFloat("_GlowVal");
        float newGlow = Mathf.MoveTowards(
            current,
            targetGlow,
            glowLerpSpeed * Time.fixedDeltaTime
        );

        Color emission = glowColor * Mathf.Lerp(glowOffIntensity, glowOnIntensity, newGlow);

        mpb.SetColor("_EmissionColor", emission);
        mpb.SetFloat("_GlowVal", newGlow);

        r.material.EnableKeyword("_EMISSION");
        r.SetPropertyBlock(mpb);
    }


    // -------------------------------------------------------------------
    // 施加悬浮力
    // -------------------------------------------------------------------
    void ApplyHover(Rigidbody rb, float infl)
    {
        Vector3 up = transform.up.normalized;
        Vector3 toBall = rb.position - transform.position;

        float height = Vector3.Dot(toBall, up);
        float error = hoverHeight - height;

        float vVert = Vector3.Dot(rb.linearVelocity, up);
        Vector3 lat = Vector3.ProjectOnPlane(toBall, up);
        Vector3 vLat = Vector3.ProjectOnPlane(rb.linearVelocity, up);

        float gravityComp = rb.mass * Physics.gravity.magnitude;
        float fUpScalar = kP * error - kD * vVert + gravityComp;
        fUpScalar = Mathf.Clamp(fUpScalar, -maxUpForce, maxUpForce);

        Vector3 fUp = up * fUpScalar;
        Vector3 fL = (-kLat * lat) + (-kLatD * vLat);
        if (fL.magnitude > maxLatForce) fL = fL.normalized * maxLatForce;

        rb.AddForce((fUp + fL) * infl, ForceMode.Force);
    }

    // -------------------------------------------------------------------
    // Launch Sequence
    // -------------------------------------------------------------------
    IEnumerator LaunchSequence()
    {
        float oH = hoverHeight;
        float oKP = kP, oKD = kD, oKLat = kLat, oKLatD = kLatD;

        float tLow = hoverHeight + prepHeightOffset;

        // A: soft sink
        float t0 = Time.time;
        while (Time.time - t0 < prepDuration)
        {
            float t = (Time.time - t0) / prepDuration;
            float s = t * t * (3f - 2f * t);

            hoverHeight = Mathf.Lerp(oH, tLow, s);
            kP = Mathf.Lerp(oKP, oKP * prepKPMult, s);
            kD = Mathf.Lerp(oKD, oKD * prepKDMult, s);
            kLat = Mathf.Lerp(oKLat, oKLat * prepKLatMult, s);
            kLatD = Mathf.Lerp(oKLatD, Mathf.Max(oKLatD, oKLatD * 1.2f), s);

            yield return new WaitForFixedUpdate();
        }

        // B: wait for stabilization
        float waitStart = Time.time;
        while (Time.time - waitStart < centerTimeout)
        {
            if (AnyBallCentered(tLow)) break;
            yield return new WaitForFixedUpdate();
        }

        // C: launch up
        foreach (var rb in balls)
            rb.AddForce(transform.up * launchImpulse, ForceMode.VelocityChange);

        kP = oKP * launchBoostKPMult;
        kD = oKD * launchBoostKDMult;
        hoverHeight = oH;

        yield return new WaitForSeconds(launchBoostTime);

        hoverHeight = oH;
        kP = oKP;
        kD = oKD;
        kLat = oKLat;
        kLatD = oKLatD;

        _launchRoutine = null;
    }

    bool AnyBallCentered(float targetLow)
    {
        foreach (var rb in balls)
        {
            Vector3 up = transform.up;
            Vector3 toBall = rb.position - transform.position;

            float h = Vector3.Dot(toBall, up);
            float radial = Vector3.ProjectOnPlane(toBall, up).magnitude;

            float vV = Vector3.Dot(rb.linearVelocity, up);
            float vL = Vector3.ProjectOnPlane(rb.linearVelocity, up).magnitude;

            bool cen =
                radial <= centerPosThreshold &&
                Mathf.Abs(targetLow - h) <= centerHeightThreshold &&
                Mathf.Abs(vV) <= centerSpeedThreshold &&
                vL <= centerSpeedThreshold;

            if (cen) return true;
        }
        return false;
    }

    // region smooth helpers
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
        float size = Mathf.Max(0.001f, regionHeightMax - regionHeightMin);
        float low = Hmin - size * edge;
        if (h <= low) return 0f;
        if (h >= Hmin) return 1f;
        float t = Mathf.InverseLerp(low, Hmin, h);
        return t * t * (3f - 2f * t);
    }
    float SmoothBand01Above(float h, float Hmax, float edge)
    {
        if (edge <= 0f) return h <= Hmax ? 1f : 0f;
        float size = Mathf.Max(0.001f, regionHeightMax - regionHeightMin);
        float high = Hmax + size * edge;
        if (h >= high) return 0f;
        if (h <= Hmax) return 1f;
        float t = Mathf.InverseLerp(high, Hmax, h);
        return t * t * (3f - 2f * t);
    }
}
