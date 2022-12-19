using Azure.Communication;
using Azure.Communication.Identity;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Azure;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace TextToAudioApp
{
    class Program
    {
        public static string key;
        public static string region;
        public static string customMessage;
        public static string fileName;
        static async Task Main(string[] args)
        {
            Console.WriteLine("Enter CognitiveServiceKey ,region and fileName values:");
            var userInput = Console.ReadLine().Split();
            key = userInput[0];
            region = userInput[1];
            fileName = userInput[2];
            var name = VerifyFileName(fileName);
            Console.WriteLine("Enter customMessage : ");
            bool done = false;
            do
            {
                customMessage = Console.ReadLine();
                await GenerateCustomAudioMessage(name);
                ConsoleKeyInfo info = Console.ReadKey(true);
                if (info.Key == ConsoleKey.Escape)
                {
                    done = true;
                }
                if (info.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine("Enter custome message:");
                }
                if (info.Key == ConsoleKey.N)
                {
                    Console.WriteLine("\n Enter FileName:");
                    fileName = Console.ReadLine();
                    name = VerifyFileName(fileName);
                    Console.WriteLine("Enter custome message:");
                }
            }
            while (!done);
        }
        private static async Task GenerateCustomAudioMessage(string fileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(customMessage))
                {
                    var config = SpeechConfig.FromSubscription(key, region);
                    config.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff16Khz16BitMonoPcm);

                    var synthesizer = new SpeechSynthesizer(config, null);
                    var result = await synthesizer.SpeakTextAsync(customMessage);
                    var stream = AudioDataStream.FromResult(result);
                    await stream.SaveToWaveFileAsync($"../../../${fileName}.wav");
                    Console.WriteLine("\n Converted customMessage into audio successfully!! \n Press N to create new file or Press Enter key to continue with same file  : \n Press Escap to Stop:");
                }
                else
                {
                    Console.WriteLine("CognitiveKey or CognitiveRegion or custom messsage value is null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while generating text to speech. Exception: {ex.Message}");
            }
        }

        public static string VerifyFileName(string fileName)
        {
            if (fileName != null && File.Exists($"../../../${fileName}.wav"))
            {

                Console.WriteLine($"File name {fileName} is already existed .Press Enter to overwrite the existed file or press N to create new file");
                ConsoleKeyInfo info = Console.ReadKey(true);
                if (info.Key == ConsoleKey.N)
                {
                    Console.WriteLine("\n Enter FileName:");
                    fileName = Console.ReadLine();
                    return fileName;
                }
                if (info.Key == ConsoleKey.Enter)
                {
                    return fileName;
                }
            }
            return fileName;
        }
    }
}
