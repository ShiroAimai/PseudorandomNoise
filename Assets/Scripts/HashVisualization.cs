using System;
using System.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

using static  Unity.Mathematics.math;
public class HashVisualization : MonoBehaviour
{
   private static int
      hashesId = Shader.PropertyToID("_Hashes"),
      configId = Shader.PropertyToID("_Config");

   [SerializeField] private int seed;
   [SerializeField, Range(-2f, 2f)] private float verticalOffset = 1f;
   [SerializeField] private Mesh mesh;
   [SerializeField] private Material mat;
   [SerializeField, Range(1, 512)] private int res = 16;

   private NativeArray<uint> hashes;
   private ComputeBuffer hashesBuffer;
   private MaterialPropertyBlock propertyBlock;
   
   [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
   struct HashJob : IJobFor
   {
      [WriteOnly] public NativeArray<uint> hashes;

      public SmallXXHash hash;
      public int resolution;
      public float invResolution;
      
      public void Execute(int index)
      {
         int v = (int)floor(invResolution * index + 0.00001f);
         int u = index - (resolution/2) * v;
         v -= resolution / 2;
         
         hashes[index] = hash.Eat(u).Eat(v);;
      }
   }

   private void OnEnable()
   {
      int length = res * res;
      hashes = new NativeArray<uint>(length, Allocator.Persistent);
      hashesBuffer = new ComputeBuffer(length, sizeof(uint));
      
      new HashJob
      {
         hashes = hashes,
         hash = SmallXXHash.Seed(seed),
         resolution = res,
         invResolution =  1f/ res
      }.ScheduleParallel(hashes.Length, res, default).Complete();
      
      hashesBuffer.SetData(hashes);

      propertyBlock ??= new MaterialPropertyBlock();
      propertyBlock.SetBuffer(hashesId, hashesBuffer);
      propertyBlock.SetVector(configId, new Vector4(res, 1f / res, verticalOffset / res));
   }

   private void OnDisable()
   {
      hashes.Dispose();
      hashesBuffer.Release();
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
      Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, new Bounds(Vector3.zero, Vector3.one), hashes.Length, propertyBlock);
   }
}
