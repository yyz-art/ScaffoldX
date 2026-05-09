using ScaffoldX.Plugin.Training.Models;

namespace ScaffoldX.Plugin.Training.Services;

public interface ITrainingScriptGenerator
{
    string GenerateYoloTrainingScript(TrainingConfig config);
    string GenerateShellScript(TrainingConfig config, bool isWindows);
    string GenerateTrainingConfig(TrainingConfig config);
}
