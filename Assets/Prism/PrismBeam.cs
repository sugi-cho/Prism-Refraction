using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PrismBeam : MonoBehaviour
{
    [SerializeField] float density = 1f;
    [SerializeField] Beam[] firstBeams;
    [SerializeField] LayerMask cullingMask;
    [SerializeField] int refCount = 16;
    [SerializeField] ColorBeams[] colorBeamsArray = new ColorBeams[7];

    public void SetDensity(float val) { firstBeams[0].density = density = val; }

    [ContextMenu("initialize")]
    void Init()
    {
        firstBeams = new Beam[2];
        colorBeamsArray = new ColorBeams[7];
        for (var i = 0; i < 7; i++)
            colorBeamsArray[i].beams = new Beam[refCount];
    }

    // Start is called before the first frame update
    void Start()
    {
        transform.hasChanged = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.hasChanged)
        {
            firstBeams[0].ray.origin = transform.position;
            firstBeams[0].ray.direction = transform.forward;
            firstBeams[0].density = density;
        }
        SimlateBeams();
    }

    private void OnDrawGizmos()
    {
        foreach (var b in firstBeams)
            if (0 < b.density)
                Gizmos.DrawLine(b.ray.origin, b.hitPos);
        foreach (var colorBeams in colorBeamsArray)
            foreach (var b in colorBeams.beams)
                if (0 < b.density)
                {
                    Gizmos.color = colorBeams.color * b.density;
                    Gizmos.DrawLine(b.ray.origin, b.hitPos);
                }
    }

    void SimlateBeams()
    {
        firstBeams[1].density = 0;
        for (var i = 0; i < 7; i++)
        {
            var colorBeams = colorBeamsArray[i];
            var beams = colorBeams.beams;
            var colorRefractRate = colorBeams.refractRate;
            for (var j = 0; j < beams.Length; j++)
                beams[j].density = 0f;

            Simulate(ref firstBeams[0], ref firstBeams[1], ref beams[0], colorRefractRate);
            {
                var j = 0;
                while (j < beams.Length - 2)
                {
                    if (Simulate(ref beams[j], ref beams[++j], ref beams[++j], colorRefractRate))
                    {
                        if (beams[j].density == 0)
                            j--;
                    }
                    else
                        break;
                }
            }
        }
    }

    bool Simulate(ref Beam current, ref Beam reflect, ref Beam refract, float colorRefractRate)
    {
        RaycastHit hit;
        var ray = current.ray;
        if (current.isInside)
        {
            ray.origin = ray.GetPoint(current.prismSize);
            ray.direction *= -1f;
        }
        if (Physics.Raycast(ray, out hit, 10f, cullingMask))
        {
            var mat = hit.collider.GetComponent<PrismMaterial>();
            if (!mat)
                return false;
            var refractRate = Mathf.Lerp(1f, mat.refractRate, colorRefractRate);
            var nml = current.isInside ? hit.normal * -1 : hit.normal;
            var eta = current.isInside ? 1f / refractRate : refractRate;

            current.prismSize = refract.prismSize = reflect.prismSize = mat.prismSize;
            current.hitPos = hit.point;
            reflect.ray.origin = refract.ray.origin = hit.point;

            reflect.ray.direction = Vector3.Reflect(current.ray.direction, nml);
            refract.ray.direction = Refract(current.ray.direction, nml, eta);

            reflect.isInside = current.isInside;
            refract.isInside = !current.isInside;

            if (0f < refract.ray.direction.magnitude)
            {
                refract.density = current.density * mat.throughRate;
                reflect.density = current.density * (1f - mat.throughRate);
                HitTest(ref reflect);
            }
            else
            {
                refract.density = 0f;
                reflect.density = current.density;
            }
            Debug.Log("true");
            return true;
        }
        else
            current.hitPos = current.ray.GetPoint(10f);
        Debug.Log("false");
        return false;
    }

    void HitTest(ref Beam beam)
    {
        RaycastHit hit;
        var ray = beam.ray;
        if (beam.isInside)
        {
            ray.origin = ray.origin + ray.direction * beam.prismSize;
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
        public float prismSize;
    }

    [System.Serializable]
    public struct ColorBeams
    {
        public Color color;
        public float refractRate;
        public Beam[] beams;
    }
}
