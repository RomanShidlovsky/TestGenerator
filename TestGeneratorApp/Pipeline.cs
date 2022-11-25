using System.Threading.Tasks.Dataflow;
using Core;

namespace TestsGeneratorApp
{
    public class Pipeline
    {
        private readonly PipelineConfiguration _configuration;

        private TransformBlock<string, string> _readerBlock;
        private TransformManyBlock<string, FileWithContent> _generatorBlock;
        private ActionBlock<FileWithContent> _writerBlock;
        private string _savePath;

        private TestsGenerator _testsGenerator = new TestsGenerator();

        public Pipeline(PipelineConfiguration configuration, string savePath = "")
        {
            _configuration = configuration;
            _savePath = savePath;

            _readerBlock = new TransformBlock<string, string>(
                async path => await ReadFile(path),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _configuration.MaxReadingTasks });

            _generatorBlock = new TransformManyBlock<string, FileWithContent>(
                source => ProcessFile(source),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _configuration.MaxProcessingTasks });

            _writerBlock = new ActionBlock<FileWithContent>(
                fileWithContent => WriteFile(fileWithContent),
                new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = _configuration.MaxWritingTasks});

            _readerBlock.LinkTo(_generatorBlock, new DataflowLinkOptions { PropagateCompletion = true });
            _generatorBlock.LinkTo(_writerBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        public async Task PerformProcessing(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                _readerBlock.Post(file);
            }

            _readerBlock.Complete();

            await _writerBlock.Completion;
        }

        private async Task<string> ReadFile(string filePath)
        {
            string result;
            using (var streamReader = new StreamReader(filePath))
            {
                result = await streamReader.ReadToEndAsync();
            }
            return result;
        }

        private FileWithContent[] ProcessFile(string fileContent)
        {
            TestClassInfo[] testClasses = _testsGenerator.Generate(fileContent).ToArray();
            FileWithContent[] filesWithContent = new FileWithContent[testClasses.Length];
            
            for (int i = 0; i < testClasses.Length; i++)
            {
                filesWithContent[i] = new FileWithContent(_savePath + "\\" + testClasses[i].Name + ".cs", testClasses[i].Code);
            }
            return filesWithContent;
        }

        private async Task WriteFile(FileWithContent fileWithContent)
        {
            using (var streamWriter = new StreamWriter(fileWithContent.Path))
            {
                await streamWriter.WriteAsync(fileWithContent.Content);
            }
        }
    }
}
