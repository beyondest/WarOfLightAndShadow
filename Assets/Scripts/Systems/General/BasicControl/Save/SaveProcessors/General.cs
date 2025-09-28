using System.Collections.Generic;
using System.Threading.Tasks;

namespace SparFlame.Systems.General.BasicControl
{
    public interface ISavePreProcessor
    {
        Task Run(object args);
    }

    public class SavePreProcessorFactory
    {
        private readonly Dictionary<SaveArcheType, ISavePreProcessor> _preProcessors = new();

        public SavePreProcessorFactory()
        {
            _preProcessors[SaveArcheType.CityUnit] = new CityUnitSavePreProcessor();
            _preProcessors[SaveArcheType.ArmyGroupUnit] = new ArmyGroupUnitPreProcessor();
            _preProcessors[SaveArcheType.CityBuilding] = new BuildingSavePreProcessor();
            _preProcessors[SaveArcheType.GameMain] = new GameMainSavePreProcessor();
            _preProcessors[SaveArcheType.City] = new CitySavePreProcessor();
            _preProcessors[SaveArcheType.ArmyGroup] = new ArmyGroupSavePreProcessor();
        }

        public ISavePreProcessor GetPreProcessor(SaveArcheType type)
        {
            return _preProcessors[type];
        }
    }

    public class SavePostProcessorFactory
    {
        private readonly Dictionary<SaveLoadTaskType, ISavePostProcessor> _postProcessors = new();

        public SavePostProcessorFactory()
        {
            _postProcessors.Add(SaveLoadTaskType.ArmyGroupSubData, new ArmyGroupSubDataPostProcessor());
            _postProcessors.Add(SaveLoadTaskType.CitySubData, new CitySubDataPostProcessor());
            _postProcessors.Add(SaveLoadTaskType.GameMainData, new GameMainDataPostProcessor());
        }

       
        public ISavePostProcessor GetPostProcessor(SaveLoadTaskType type)
        {
            return _postProcessors[type];
        }
    }

    public interface ISavePostProcessor
    {
        Task Run(object args);
    }
}