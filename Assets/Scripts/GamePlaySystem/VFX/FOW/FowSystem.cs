using System.Collections.Generic;
using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;


namespace SparFlame.GamePlaySystem.Fow
{
    [UpdateAfter(typeof(CalAgentInfoSystem))]
    public partial class FowSystem : SystemBase
    {
        private static readonly int SamplingRange = Shader.PropertyToID("_SamplingRange");
        private static readonly int FOVMap = Shader.PropertyToID("_FOVMap");
        private static readonly int LayerCount = Shader.PropertyToID("_LayerCount");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int InputTexture = Shader.PropertyToID("inputTexture");
        private static readonly int OutputBuffer = Shader.PropertyToID("outputBuffer");
        private static readonly int AgentCount = Shader.PropertyToID("_AgentCount");
        private static readonly int Positions = Shader.PropertyToID("_Positions");
        private static readonly int Forwards = Shader.PropertyToID("_Forwards");
        private static readonly int Ranges = Shader.PropertyToID("_Ranges");
        private static readonly int AngleCosines = Shader.PropertyToID("_AngleCosines");
        private static readonly int PlaneSizeX = Shader.PropertyToID("_PlaneSizeX");
        private static readonly int PlaneSizeZ = Shader.PropertyToID("_PlaneSizeZ");
        private static readonly int FowColor = Shader.PropertyToID("_FOWColor");
        private static readonly int BlockOffset = Shader.PropertyToID("_BlockOffset");
        private static readonly int PlanePos = Shader.PropertyToID("_PlanePos");
        private static readonly int PlaneRight = Shader.PropertyToID("_PlaneRight");
        private static readonly int PlaneForward = Shader.PropertyToID("_PlaneForward");
        private static readonly int PlaneScale = Shader.PropertyToID("_PlaneScale");
        private static readonly int Sigma = Shader.PropertyToID("_Sigma");
        private static readonly int Direction = Shader.PropertyToID("_Direction");
        private static readonly int TargetAgentCount = Shader.PropertyToID("targetAgentCount");
        private static readonly int TargetAgentUVs = Shader.PropertyToID("targetAgentUVs");

        // Internal Data
        private bool _initialized;
        private float _updateTime;
        private float4 _currentColor;

        private RenderTexture _fowRenderTexture;
        private Material _blurMaterial;
        private Material _fovMaterial; // Field of view material
        private Material _fowMaterial; // Fog of war material


        // Agent visibility
        private ComputeBuffer _outputAlphaBuffer;
        private int _kernelID;
        private bool _needAgentVisibilityUpdate = true;
        private NativeList<Entity> _visibilityTargetAgents; // This is only cache, update runtime

        // Agents status
        private NativeList<float3> _positions;
        ComputeBuffer _positionsBuffer;
        private NativeList<float3> _forwards;
        ComputeBuffer _forwardsBuffer;
        private NativeList<float> _ranges;
        ComputeBuffer _rangesBuffer;
        private NativeList<float> _angleCosines;
        ComputeBuffer _angleCosinesBuffer;


        // Shaders and materials
        private Texture2DArray _fovMapArray;


        private Shader _gaussianShader;

        private ComputeShader _pixelReader;


        protected override void OnCreate()
        {
            RequireForUpdate<GameStatusData>();
            RequireForUpdate<FowConfig>();
        }


        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                var config = SystemAPI.GetSingleton<FowConfig>();
                Initialize(config);
            }
        }

        protected override void OnUpdate()
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>();
            if (gameStatusData.Value == GameStatus.Init)
            {
                LateInitialize();
                return;
            }
            if(gameStatusData.Value != GameStatus.Gaming)return;
            
            var mousePositionInfoEntity = SystemAPI.GetSingletonEntity<MousePositionFowTag>();
            var inputMouseData = SystemAPI.GetSingleton<InputMouseData>();
            EntityManager.SetComponentData(mousePositionInfoEntity, new LocalTransform
            {
                Position = inputMouseData.HitPosition,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            var config = SystemAPI.GetSingleton<FowConfig>();
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            if (curTime <= _updateTime) return;
           
            _updateTime = curTime + config.UpdateInterval;
            UpdateContributorSight(config);
            if (_needAgentVisibilityUpdate)
                UpdateDisappearer(
                    config); // Call before ApplyFOWPass, as calling it after the pass will stall the main thread for a while.
            ApplyFowPass(config);
        }


        protected override void OnDestroy()
        {
           DeInitialize();
        }

        private void DeInitialize()
        {
            _fowRenderTexture?.Release();
            _positionsBuffer?.Release();
            _forwardsBuffer?.Release();
            _rangesBuffer?.Release();
            _angleCosinesBuffer?.Release();
            _outputAlphaBuffer?.Release();
            if (_visibilityTargetAgents.IsCreated)
                _visibilityTargetAgents.Dispose();
            if (_positions.IsCreated)
                _positions.Dispose();
            if (_forwards.IsCreated)
                _forwards.Dispose();
            if (_ranges.IsCreated)
                _ranges.Dispose();
            if (_angleCosines.IsCreated)
                _angleCosines.Dispose();
        }

        private void LateInitialize()
        {
            _updateTime = 0;
            var config = SystemAPI.GetSingleton<FowConfig>();
            _fovMapArray = FogOfWarGo.Instance.fovMapArray;
            _pixelReader = FogOfWarGo.Instance.pixelReader;
            if(_fovMaterial != null)
                Object.Destroy(_fovMaterial);
            if(_fowMaterial != null)
                Object.Destroy(_fowMaterial);
            if(_blurMaterial != null)
                Object.Destroy(_blurMaterial);
            _fovMaterial = new Material(FogOfWarGo.Instance.fovShader);
            _fowMaterial = new Material(FogOfWarGo.Instance.fowProjectorShader);
            _blurMaterial = new Material(FogOfWarGo.Instance.gaussianShader);
            FogOfWarGo.Instance.SetMaterial(_fowMaterial);
            _fovMaterial.SetFloat(SamplingRange, _fovMapArray.mipMapBias);
            _fovMaterial.SetTexture(FOVMap, _fovMapArray);
            _fovMaterial.SetInt(LayerCount, _fovMapArray.depth);
            _fowMaterial.SetTexture(MainTex, _fowRenderTexture); // It will be projected using a Plane.
            _kernelID = _pixelReader.FindKernel("ReadPixels");
            _pixelReader.SetTexture(_kernelID, InputTexture, _fowRenderTexture);
            _pixelReader.SetBuffer(_kernelID, OutputBuffer, _outputAlphaBuffer);
            _currentColor = SystemAPI.GetSingleton<PlayerFactionData>().Value == FactionTag.Ally
                ? config.LightFowColor
                : config.DarkFowColor;
        }
        
        private void Initialize(in FowConfig config)
        {
            _visibilityTargetAgents = new NativeList<Entity>(Allocator.Persistent);
            _positions = new NativeList<float3>(Allocator.Persistent);
            _forwards = new NativeList<float3>(Allocator.Persistent);
            _ranges = new NativeList<float>(Allocator.Persistent);
            _angleCosines = new NativeList<float>(Allocator.Persistent);
            
            _fowRenderTexture =
                new RenderTexture(config.FowTextureSize, config.FowTextureSize, 1, RenderTextureFormat.ARGB32);
            // _outputAlphaBuffer =
            //     new ComputeBuffer(1, sizeof(float) * config.MaxEnemyCount, ComputeBufferType.IndirectArguments);
            _outputAlphaBuffer = new ComputeBuffer(config.MaxEnemyCount, sizeof(float),ComputeBufferType.Default);
            _positionsBuffer = new ComputeBuffer(config.MaxAllyCount, sizeof(float) * 3,
                ComputeBufferType.IndirectArguments);
            _forwardsBuffer = new ComputeBuffer(config.MaxAllyCount, sizeof(float) * 3,
                ComputeBufferType.IndirectArguments);
            _rangesBuffer = new ComputeBuffer(config.MaxAllyCount, sizeof(float), ComputeBufferType.IndirectArguments);
            _angleCosinesBuffer =
                new ComputeBuffer(config.MaxAllyCount, sizeof(float), ComputeBufferType.IndirectArguments);
            
            _initialized = true;
        }


        // Aggregate the status of agents and transfer to the GPU 
        void UpdateContributorSight(in FowConfig config)
        {
            // Set agents' data
            _positions.Clear();
            _ranges.Clear();
            _forwards.Clear();
            _angleCosines.Clear();

            var count = 0;
            foreach (var agent in SystemAPI.Query<FowAgent>().WithAll<InCameraExtendView>()
                         .WithAll<ContributeSightTag>())
            {
                if (count == config.MaxAllyCount) break;
                count++;
                _positions.Add(agent.RelativePosition);
                _forwards.Add(agent.RelativeForward);
                _ranges.Add(agent.SightRange);
                _angleCosines.Add(agent.SightCos);
            }

            _fovMaterial.SetInt(AgentCount, count);

            // Bind buffers
            _positionsBuffer.SetData(_positions.AsArray());
            _forwardsBuffer.SetData(_forwards.AsArray());
            _rangesBuffer.SetData(_ranges.AsArray());
            _angleCosinesBuffer.SetData(_angleCosines.AsArray());

            _fovMaterial.SetBuffer(Positions, _positionsBuffer);
            _fovMaterial.SetBuffer(Forwards, _forwardsBuffer);
            _fovMaterial.SetBuffer(Ranges, _rangesBuffer);
            _fovMaterial.SetBuffer(AngleCosines, _angleCosinesBuffer);

            // Set uniform values for FOVMaterial
            _fovMaterial.SetFloat(PlaneSizeX, config.LossyScale.x);
            _fovMaterial.SetFloat(PlaneSizeZ, config.LossyScale.z);

            _fovMaterial.SetColor(FowColor,
                new Color(_currentColor.x, _currentColor.y, _currentColor.z, _currentColor.w));
            _fovMaterial.SetFloat(BlockOffset, config.BlockOffset);

            // Set uniform values for FOWMaterial
            _fowMaterial.SetVector(PlanePos, (Vector3)config.Position);
            _fowMaterial.SetVector(PlaneRight, (Vector3)config.Right);
            _fowMaterial.SetVector(PlaneForward, (Vector3)config.Forward);
            _fowMaterial.SetVector(PlaneScale, (Vector3)config.LocalScale);
        }

        // Apply FOVMapping and Gaussian blur passes to a RenderTexture
        void ApplyFowPass(in FowConfig config)
        {
            var backup = RenderTexture.active;
            RenderTexture.active = _fowRenderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = backup;

            Graphics.Blit(null, _fowRenderTexture, _fovMaterial); // Render FOV to FOWRenderTexture

            // Blur
            var temp = RenderTexture.GetTemporary(_fowRenderTexture.width, _fowRenderTexture.height, 0,
                _fowRenderTexture.format);
            _blurMaterial.SetFloat(Sigma, config.Sigma);

            // Apply Gaussian blur shader multiple times
            // Render to one another alternately

            for (var i = 0; i < config.BlurIterationCount; ++i)
            {
                _blurMaterial.SetVector(Direction, new Vector2(1, 0));
                Graphics.Blit(_fowRenderTexture, temp, _blurMaterial);
                _blurMaterial.SetVector(Direction, new Vector2(0, 1));
                Graphics.Blit(temp, _fowRenderTexture, _blurMaterial);
            }

            RenderTexture.ReleaseTemporary(temp);
        }


        void UpdateDisappearer(in FowConfig config)
        {
            _needAgentVisibilityUpdate = false;

            _visibilityTargetAgents.Clear();
            var targetAgentUVs = new List<Vector4>();

            var count = 0;
            foreach (var agent in SystemAPI.Query<FowAgent>().WithAll<InCameraExtendView>()
                         .WithAll<DisappearInFowTag>())
            {
                if (count == config.MaxEnemyCount) break;
                count++;
                _visibilityTargetAgents.Add(agent.Self);
                targetAgentUVs.Add(agent.UV);
            }

            // Use the compute shader to retrieve pixel data from the GPU to CPU
            _pixelReader.SetInt(TargetAgentCount, _visibilityTargetAgents.Length);
            _pixelReader.SetVectorArray(TargetAgentUVs, targetAgentUVs.ToArray());
            _pixelReader.Dispatch(_kernelID, 1, 1, 1);

            // Asynchronous request to the GPU
            AsyncGPUReadback.Request(_outputAlphaBuffer, OnRetrieveAgentVisibility);
        }

        private void OnRetrieveAgentVisibility(AsyncGPUReadbackRequest request)
        {
            if (!_visibilityTargetAgents.IsCreated) return;

            var
                alphaSamples =
                    request.GetData<float>().ToArray(); // Sampling results of the FOW at the locations of agents

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            // Set visibility
            for (var i = 0; i < _visibilityTargetAgents.Length; ++i)
            {
                var entity = _visibilityTargetAgents[i];
                if(!EntityManager.HasComponent<FowAgentData>(entity))continue;
                var agent = EntityManager.GetAspect<FowAgent>(entity);
                var isInSight = alphaSamples[i] <= agent.DisappearAlphaThreshold;
                agent.SetUnderFow(isInSight, ecb);
            }

            _needAgentVisibilityUpdate = true;
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}