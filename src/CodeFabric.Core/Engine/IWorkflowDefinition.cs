
namespace CodeFabric.Core.Engine;

public interface IWorkflowDefinition
{
   string WorkflowName { get; }
   WorkflowStep GetNetStep(WorkflowStep currentStep);
}
