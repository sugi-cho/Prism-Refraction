using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PrismBeam : MonoBehaviour
{
    [SerializeField] float density = 1f;
    [SerializeField] float prismSize = 1f;
    [SerializeField] float refractRate = 1.45f;
    [SerializeField] float throughRate = 0.8f;
    [SerializeField] Beam[] beams;
    [SerializeField] LayerMask cullingMask;

    public void SetDensity(float val) { beams[0].density = density = val; }

    // Start is called before the first frame update
    void Start()
    {
        beams = new Beam[6];
        transform.hasChanged = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            beams[0].ray.origin = transform.position;
            beams[0].ray.direction = transform.forward;
            beams[0].density = density;
        }
        SimlateBeams();
    }

    private void OnDrawGizmos()
    {
        foreach (var b in beams)
            if (0 < b.density)
            {
                Gizmos.color = b.isRefract ? Color.red : Color.white;
                Gizmos.DrawLine(b.ray.origin, b.hitPos);
            }
    }

    void SimlateBeams()
    {
        var i = 0;
        while (i < beams.Length - 2)
        {
            if (Simulate(ref beams[i], ref beams[++i], ref beams[++i]))
            {
                if (beams[i].density == 0)
                    i--;
            }
            else
            {
                i--;
                break;
            }
        }
        for (var j = i; j < beams.Length; j++)
            beams[j].density = 0;
    }

    bool Simulate(ref Beam current, ref Beam reflect, ref Beam refract)
    {
        RaycastHit hit;
        var ray = current.ray;
        if (current.isInside)
        {
            ray.origin = ray.origin + ray.direction * prismSize;
            ray.direction *= -1f;
        }
        if (Physics.Raycast(ray, out hit, 10f, cullingMask))
        {
            var nml = current.isInside ? hit.normal * -1 : hit.normal;
            var eta = current.isInside ? 1f / refractRate : refractRate;

            current.hitPos = hit.point;
            reflect.ray.origin = refract.ray.origin = hit.point;

            reflect.ray.direction = Vector3.Reflect(current.ray.direction, nml);
            refract.ray.direction = Refract(current.ray.direction, nml, eta);

            reflect.isInside = current.isInside;
            refract.isInside = !current.isInside;

            refract.isRefract = true;
            reflect.isRefract = false;
            if (0f < refract.ray.direction.magnitude)
            {
                refract.density = current.density * throughRate;
                reflect.density = current.density * (1f - throughRate);
                HitTest(ref reflect);
            }
            else
            {
                refract.density = 0f;
                reflect.density = current.density;
            }
            return true;
        }
        else
            current.hitPos = current.ray.GetPoint(10f);
        return false;
    }

    void HitTest(ref Beam beam)
    {
        RaycastHit hit;
        var ray = beam.ray;
        if (beam.isInside)
        {
            ray.origin = ray.origin + ray.direction * prismSize;
            ray.direction *= -1f;
        }
        if (Physics.Raycast(ray, out hit, 100f, cullingMask))
            beam.hitPos = hit.point;
        else
            beam.hitPos = beam.ray.GetPoint(10f);
    }

    //https://developer.download.nvidia.com/cg/refract.html
    Vector3 Refract(Vector3 i, Vector3 n, float eta)
    {
        float cosi = Vector3.Dot(-i, n);
        float cost2 = 1.0f - eta * eta * (1.0f - cosi * cosi);
        Vector3 t = eta * i + ((eta * cosi - Mathf.Sqrt(Mathf.Abs(cost2))) * n);
        return t * (cost2 > 0 ? 1f : 0f);
    }

    [System.Serializable]
    public struct Beam
    {
        public Ray ray;
        public float density;
        public Vector3 hitPos;
        public bool isInside;
        public bool isRefract;
    }
}
