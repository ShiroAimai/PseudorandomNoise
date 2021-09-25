using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using static  Unity.Mathematics.math;
public class HashSpace : MonoBehaviour
{
   private static int
      hashesId = Shader.PropertyToID("_Hashes"),
      positionsId = Shader.PropertyToID("_Positions"),
      normalsId = Shader.PropertyToID("_Normals"),
      configId = Shader.PropertyToID("_Config");

   public enum Shape { Plane, Sphere, Octahedron, Torus }

   static Shapes.ScheduleDelegate[] shapeJobs = {
         Shapes.Job<Shapes.Plane>.ScheduleParallel,
         Shapes.Job<Shapes.Sphere>.ScheduleParallel,
         Shapes.Job<Shapes.Octahedron>.ScheduleParallel,
         Shapes.Job<Shapes.Torus>.ScheduleParallel
   };
   
   [SerializeField]
   Shape shape;
   
   [SerializeField] private int seed;
   [SerializeField, Range(-0.5f, 0.5f)] private float singleInstanceDisplacement = 0.1f;
   [SerializeField, Range(0.1f, 10f)] private float singleInstanceScale = 1f;
   [SerializeField, Range(1, 512)] private int res = 16;
   [SerializeField] private Mesh mesh;
   [SerializeField] private Material mat;
   [SerializeField] private SpaceTRS domain = new SpaceTRS {scale = 8f};
   
   private NativeArray<uint4> hashes;
   private NativeArray<float3x4> positions, normals;
   private ComputeBuffer hashesBuffer, positionsBuffer, normalsBuffer;
   private MaterialPropertyBlock propertyBlock;

   private bool isDirty;
   private Bounds bounds;
   
   [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
   struct HashJob : IJobFor
   {
      [WriteOnly] public NativeArray<uint4> hashes;
      [ReadOnly] public NativeArray<float3x4> positions;
      
      float4x3 TransformPos(float3x4 trs, float4x3 pos) => float4x3(
         trs.c0.x * pos.c0 + trs.c1.x * pos.c1 + trs.c2.x * pos.c2 + trs.c3.x,
         trs.c0.y * pos.c0 + trs.c1.y * pos.c1 + trs.c2.y * pos.c2 + trs.c3.y,
         trs.c0.z * pos.c0 + trs.c1.z * pos.c1 + trs.c2.z * pos.c2 + trs.c3.z
      );
      
      public SmallXXHash4 hash;
      public float3x4 domainTRS;
      
      public void Execute(int index)
      {
         float4x3 pos = TransformPos(domainTRS, transpose(positions[index])); 
         int4 u = (int4) floor(pos.c0);
         int4 v = (int4) floor(pos.c1);
         int4 w = (int4) floor(pos.c2);
         hashes[index] = hash.Eat(u).Eat(v).Eat(w);;
      }
   }

   private void OnEnable()
   {
      isDirty = true;
      
      int length = res * res;
      length = length / 4 + (length & 1);
      
      hashes = new NativeArray<uint4>(length, Allocator.Persistent);
      positions = new NativeArray<float3x4>(length, Allocator.Persistent);
      normals = new NativeArray<float3x4>(length, Allocator.Persistent);
      hashesBuffer = new ComputeBuffer(length * 4, sizeof(uint));
      positionsBuffer = new ComputeBuffer(length * 4, 3 * 4);
      normalsBuffer = new ComputeBuffer(length * 4, 3 * 4);
      
      propertyBlock ??= new MaterialPropertyBlock();
      propertyBlock.SetBuffer(hashesId, hashesBuffer);
      propertyBlock.SetBuffer(positionsId, positionsBuffer);
      propertyBlock.SetBuffer(normalsId, normalsBuffer);
      propertyBlock.SetVector(configId, new Vector4(res, singleInstanceScale / res, singleInstanceDisplacement));
   }

   private void OnDisable()
   {
      hashes.Dispose();
      positions.Dispose();
      normals.Dispose();
      hashesBuffer.Release();
      positionsBuffer.Release();
      normalsBuffer.Release();
      normalsBuffer = null;
      positionsBuffer = null;
      hashesBuffer = null;
   }

   private void OnValidate()
   {
      if (hashesBuffer == null || !enabled) return;
      OnDisable();
      OnEnable();
   }

   private void Update()
   {
      if (isDirty || transform.hasChanged)
      {
         isDirty = false;
         transform.hasChanged = false;
         bounds = new Bounds(transform.position, float3(2f * cmax(abs(transform.lossyScale)) + singleInstanceDisplacement));
         JobHandle handle = shapeJobs[(int) shape](
            positions, normals, transform.localToWorldMatrix, res, default
         );

         new HashJob {
            positions = positions,
            hashes = hashes,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix
         }.ScheduleParallel(hashes.Length, res, handle).Complete();

         hashesBuffer.SetData(hashes.Reinterpret<uint>(4 * 4));
         positionsBuffer.SetData(positions.Reinterpret<float3>(3 * 4 * 4));
         normalsBuffer.SetData(normals.Reinterpret<float3>(3 * 4 * 4));
      }
      
      Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, bounds, res * res, propertyBlock);
   }
}
