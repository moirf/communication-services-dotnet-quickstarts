using Microsoft.CognitiveServices.Speech;

namespace TextToAudioApp
{
    internal class Program
    {
        private static string? key;
        private static string? region;
        private static SpeechConfig? speechConfig;

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Provide Cognitive Service Key and Region (eg. abcxyz eastus:");
            string[] inputs = Console.ReadLine().Split();

            if (inputs.Length != 2)
            {
                Console.WriteLine("Usage: Cognitive Service Key Region of Cognitive service. \n eg. <CognitiveSeviceKey> <eastus>");
                return;
            }

            key = inputs[0];
            region = inputs[1];

            //set speech config
            speechConfig = SpeechConfig.FromSubscription(key, region);

            while (true)
            {
                Console.WriteLine("Provide custom text message to convert to speech:");
                var text = Console.ReadLine();

                if (text == null)
                {
                    return;
                }
                if(await ConvertTextToSpeech(text))
                {
                    Console.WriteLine("File saved, Hit Esc Key to Exit, Press any other key to continiue.");
                    if(Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }
            }
        }
        private static async Task<bool> ConvertTextToSpeech(string text)
        {
            try
            {
                if (speechConfig == null)
                {
                    Console.WriteLine($"Configuration issue in cognitive service, close the app and retry.");
                    return false;
                }

                speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

                var synthesizer = new SpeechSynthesizer(speechConfig, null);
                var result = await synthesizer.SpeakTextAsync(text);
                var stream = AudioDataStream.FromResult(result);

                Console.Write("Text to speach converted, Enter the file name to save: (no space or special characters:");
                var fileName = Console.ReadLine();
                await stream.SaveToWaveFileAsync($"../../../${fileName}.wav");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while generating text to speech. Exception: {ex.Message}");
                return false;
            }
        }
    }
}
