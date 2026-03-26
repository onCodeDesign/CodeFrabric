using CodeFabric.Core.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeFabric.Core.Definitions
{
   internal class FeatureWorkflow : IWorkflowDefinition
   {
      public string WorkflowName => nameof(FeatureWorkflow);

      public WorkflowStep GetNetStep(WorkflowStep currentStep)
       {
           throw new NotImplementedException();
       }
   }
}
