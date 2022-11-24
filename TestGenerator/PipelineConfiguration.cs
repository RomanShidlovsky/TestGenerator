using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGenerator
{
    public class PipelineConfiguration
    {
        public const int DefaultReadingTasks = 5;
        public const int DefaultProcessigTasks = 5;
        public const int DefaultWritingTasks = 5;
        public int MaxReadingTasks { get; }
        public int MaxProcessingTasks { get; }
        public int MaxWritingTasks { get; }

        public PipelineConfiguration(int maxReadingTasks, int maxProcessingTasks, int maxWritingTasks)
        {
            MaxReadingTasks = maxReadingTasks;
            MaxProcessingTasks = maxProcessingTasks;
            MaxWritingTasks = maxWritingTasks;
        }
    }
}
