using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FoliageSpawner : MonoBehaviour
{
    struct MeshTypeList
    {
        public Mesh mesh;
        public Material material;

        public List<Matrix4x4> transforms;

        public MeshTypeList(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;

            transforms = new List<Matrix4x4>();
        }

        public void ClearTransforms()
        {
            transforms.Clear();
        }
    }

    [Header("Trees")]
    public List<GameObject> treePrefabs;
    public Gradient treeGradient;

    [Header("Grass")]
    public List<GameObject> grassPrefabs;
    public List<Mesh> grassMeshes;
    public Material grassMaterial;
    public Gradient grassGradient;

    [Header("Flowers")]
    public List<GameObject> flowerPrefabs;
    public List<Mesh> flowerMeshes;
    public Material flowerMaterial;
    

    [HideInInspector] public bool drawGrass = true;
    [HideInInspector] public bool drawFlowers = true;

    List<MeshTypeList> grassCombinedRender = new List<MeshTypeList>();
    List<MeshTypeList> flowersCombinedRender = new List<MeshTypeList>();

    private void Update()
    {
        if (drawGrass)
            DrawMeshesInChunks(grassCombinedRender);

        if (drawFlowers)
            DrawMeshesInChunks(flowersCombinedRender);
    }


    public IEnumerator GenerateTrees(
    float xsize,
    float ysize,
    float minPointRadius,
    float distanceFromEdges,
    List<Vector3> pointsToAvoid,
    float pointsAvoidDistance,
    List<GameObject> toDelete,
    bool animate = false,
    int seed = 100)
    {
        RandomNumbers rng = new RandomNumbers(seed);

        RaycastHit hit;

        var samples = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges), seed: seed);

        //Add trees
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % Mathf.CeilToInt(40 * Time.deltaTime) == 0 && animate)
                yield return new WaitForSeconds(.02f);

            // Doing it every loop so seed is not depending on amount of spawned objects
            var rot = Quaternion.Euler(rng.Range(-5, 5), rng.Range(0, 360), rng.Range(-5, 5));
            var scale = new Vector3(rng.Range(1.1f, 1.4f), rng.Range(1.1f, 1.4f), rng.Range(1.1f, 1.4f));
            var color = treeGradient.Evaluate(rng.Range(0f, 1f));
            var prefab = treePrefabs[rng.Range(0, treePrefabs.Count)];

            var rayStartPos = new Vector3(samples[i].x + distanceFromEdges/2, 100, samples[i].y + distanceFromEdges/2);

            if (Physics.Raycast(rayStartPos, -Vector3.up, out hit))
            {
                bool spawn = true;

                // Check if not on avoid point
                foreach (var point in pointsToAvoid)
                {
                    if ((hit.point - point).sqrMagnitude <= pointsAvoidDistance * pointsAvoidDistance)
                    {
                        spawn = false;
                    }
                }

                // Check if slope is not too big
                if (hit.normal.y < .8f)
                {
                    spawn = false;
                }

                if (spawn)
                {
                    toDelete.Add(Spawn(prefab, hit.point + Vector3.up, rot, scale, color, animate));
                }
            }
        }
    }


    public IEnumerator GenerateGrass(
    float xsize,
    float ysize,
    float minPointRadius,
    float distanceFromEdges,
    List<Vector3> pointsToAvoid,
    float pointsAvoidDistance,
    List<GameObject> toDelete,
    bool animate = false,
    int seed = 100)
    {
        RandomNumbers rng = new RandomNumbers(seed);

        foreach (var item in grassCombinedRender) 
            item.ClearTransforms();

        foreach (var mesh in grassMeshes)
        {
            Material mat = new Material(grassMaterial);
            mat.color = grassGradient.Evaluate(rng.Range(0f, 1f));
            grassCombinedRender.Add(new MeshTypeList(mesh, mat));
        }

        RaycastHit hit;

        var samples = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges), seed: seed);

        //Add grass
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % Mathf.CeilToInt(40 * Time.deltaTime) == 0 && animate)
                yield return new WaitForSeconds(.02f);

            // Setting random parameters every loop so seed is not depending on amount of spawned objects
            var scale = new Vector3(rng.Range(70, 80), rng.Range(70, 80), rng.Range(70, 80));
            int rndModel = rng.Range(0, grassMeshes.Count);
            var prefab = grassPrefabs[rndModel];

            var rayStartPos = new Vector3(samples[i].x + distanceFromEdges / 2, 100, samples[i].y + distanceFromEdges / 2);

            if (Physics.Raycast(rayStartPos, -Vector3.up, out hit))
            {
                bool spawn = true;

                // Check if not on avoid point
                foreach (var point in pointsToAvoid)
                {
                    if ((hit.point - point).sqrMagnitude <= pointsAvoidDistance * pointsAvoidDistance)
                    {
                        spawn = false;
                    }
                }

                // Check if slope is not too big
                if (hit.normal.y < .72f)
                    spawn = false;


                var rot = Quaternion.FromToRotation(Vector3.up, hit.normal);

                // Only for video
                //toDelete.Add(Spawn(prefab, hit.point + Vector3.up * -1f, rot, scale, null, animate));
                if (spawn && animate)
                    toDelete.Add(Spawn(prefab, hit.point + Vector3.up * -1f, rot, scale, null, animate));
                else if (spawn)
                    grassCombinedRender[rndModel].transforms.Add(Matrix4x4.TRS(hit.point + Vector3.up * -1f, rot, scale));
            }
        }
    }


    public IEnumerator GenerateFlowers(
    float xsize,
    float ysize,
    float minPointRadius,
    float distanceFromEdges,
    List<Vector3> pointsToAvoid,
    float pointsAvoidDistance,
    List<GameObject> toDelete,
    bool animate = false,
    int seed = 100)
    {
        RandomNumbers rng = new RandomNumbers(seed);

        foreach (var item in flowersCombinedRender) 
            item.ClearTransforms();

        foreach (var mesh in flowerMeshes)
            flowersCombinedRender.Add(new MeshTypeList(mesh, flowerMaterial));

        RaycastHit hit;

        var samples = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges), seed: seed);

        //Add grass
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % Mathf.CeilToInt(40 * Time.deltaTime) == 0 && animate)
                yield return new WaitForSeconds(.02f);

            // Setting random parameters every loop so seed is not depending on amount of spawned objects
            var scaleF = rng.Range(1.5f, 2);
            var scale = new Vector3(scaleF, scaleF, scaleF);
            int rndModel = rng.Range(0, flowerMeshes.Count);
            var prefab = flowerPrefabs[rndModel];

            var rayStartPos = new Vector3(samples[i].x + distanceFromEdges / 2, 100, samples[i].y + distanceFromEdges / 2);

            if (Physics.Raycast(rayStartPos, -Vector3.up, out hit))
            {
                bool spawn = true;

                // Check if not on avoid point
                foreach (var point in pointsToAvoid)
                {
                    if ((hit.point - point).sqrMagnitude <= pointsAvoidDistance * pointsAvoidDistance)
                    {
                        spawn = false;
                    }
                }

                // Check if slope is not too big
                if (hit.normal.y < .7f)
                    spawn = false;


                var rot = Quaternion.FromToRotation(Vector3.up, hit.normal);

                // Only for video
                //toDelete.Add(Spawn(prefab, hit.point + Vector3.up * -1f, rot, scale, null, animate));
                if (spawn && animate)
                    toDelete.Add(Spawn(prefab, hit.point + Vector3.up * -1f, rot, scale, null, animate));
                else if (spawn)
                    flowersCombinedRender[rndModel].transforms.Add(Matrix4x4.TRS(hit.point + Vector3.up * -0.05f, rot, scale));
            }
        }
    }



    private GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, Color? color, bool animate)
    {
        var obj = Instantiate(prefab, pos, rot);
        obj.transform.parent = transform;

        if (color != null)
        {
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            var mat = meshRenderer.materials[0];
            mat.color = (Color)color;
            meshRenderer.materials[0] = mat;
        }

        if (animate)
        {
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(scale, .6f);
        }
        else
        {
            obj.transform.localScale = scale;
        }

        return obj;
    }



    private void DrawMeshesInChunks(List<MeshTypeList> meshTypeList)
    {
        Matrix4x4[] matrices;

        foreach (var meshTypes in meshTypeList)
        {
            for (int chunkStart = 0; chunkStart < meshTypes.transforms.Count; chunkStart += 1024)
            {
                int count = meshTypes.transforms.Count - chunkStart;
                count = count > 1023 ? 1023 : count;

                matrices = meshTypes.transforms.GetRange(chunkStart, count).ToArray();
                
                Graphics.DrawMeshInstanced(meshTypes.mesh, 0, meshTypes.material, matrices, count);
            }
        }
    }
}
