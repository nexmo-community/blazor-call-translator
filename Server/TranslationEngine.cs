using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using VonageDotnetTranslator.Server.Hubs;
using VonageDotnetTranslator.Shared;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace VonageDotnetTranslator.Server
{
    public class TranslationEngine: IDisposable
    {
        const int SAMPLES_PER_SECOND = 16000;
        const int BITS_PER_SAMPLE = 16;
        const int NUMBER_OF_CHANNELS = 1;
        const int BUFFER_SIZE = 320 * 2;

        private ConcurrentQueue<byte[]> _audioToWrite = new ConcurrentQueue<byte[]>(); // queue to managed synthezized audio
        private readonly IConfiguration _config; //Where Azure Subscription Keys will be stored
        private readonly IHubContext<TranslationHub> _hub; // Hub connection we'll use to talk to frontend
        private string _uuid; // Unique ID of the call being translated
        private string _languageSpoken; // The language being spoken on the call
        private string _languageTranslated; // The language being translated to

        private SpeechTranslationConfig _translationConfig; // the configuration for the speech translator
        private SpeechConfig _speechConfig; // configuration for the speech synthesizer
        private PushAudioInputStream _inputStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(SAMPLES_PER_SECOND, BITS_PER_SAMPLE, NUMBER_OF_CHANNELS)); //Stream for handling audio input to the translator
        private AudioConfig _audioInput; //configuration for the translation audio
        private TranslationRecognizer _recognizer; // The translator
        private SpeechSynthesizer _synthesizer; // The syntheziser, which will turn translated text into audio
        private AudioOutputStream _audioOutputStream; // Output stream from the synthezier
        private AudioConfig _outputConfig; // output configuration for the speech syntheizer

        public TranslationEngine(IConfiguration config, IHubContext<TranslationHub> hub)
        {
            _hub = hub;
            _config = config;
            _translationConfig = SpeechTranslationConfig.FromSubscription(
                _config["SUBSCRIPTION_KEY"], _config["REGION"]);
            _speechConfig = SpeechTranslationConfig.FromSubscription(
                _config["SUBSCRIPTION_KEY"], _config["REGION"]);
            _audioInput = AudioConfig.FromStreamInput(_inputStream);
            _audioOutputStream = AudioOutputStream.CreatePullStream();
            _outputConfig = AudioConfig.FromStreamOutput(_audioOutputStream);
        }

        private void RecognizerRecognized(object sender, TranslationRecognitionEventArgs e)
        {
            var translationLanguage = _languageTranslated.Split("-")[0];
            var translation = e.Result.Translations[translationLanguage].ToString();
            Trace.WriteLine("Recognized: " + translation);
            var ttsAudio = _synthesizer.SpeakTextAsync(translation).Result.AudioData;
            var translationResult = new Translation
            {
                LanguageSpoken = _languageSpoken,
                LanguageTranslated = _languageTranslated,
                Text = translation,
                UUID = _uuid
            };
            _hub.Clients.All.SendAsync("receiveTranslation", translationResult);
            _audioToWrite.Enqueue(ttsAudio);
        }

        private async Task StartSpeechTranslationEngine(string recognitionLanguage, string targetLanguage)
        {
            _translationConfig.SpeechRecognitionLanguage = recognitionLanguage;
            _translationConfig.AddTargetLanguage(targetLanguage);
            _speechConfig.SpeechRecognitionLanguage = targetLanguage;
            _speechConfig.SpeechSynthesisLanguage = targetLanguage;
            _synthesizer = new SpeechSynthesizer(_speechConfig, _outputConfig);
            _recognizer = new TranslationRecognizer(_translationConfig, _audioInput);
            _recognizer.Recognized += RecognizerRecognized;
            await _recognizer.StartContinuousRecognitionAsync();
        }

        private async Task StopTranscriptionEngine()
        {
            if (_recognizer != null)
            {
                _recognizer.Recognized -= RecognizerRecognized;
                await _recognizer.StopContinuousRecognitionAsync();
            }
        }

        public async Task ReceiveAudioOnWebSocket(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[BUFFER_SIZE];

            try
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var config = JsonConvert.DeserializeObject<Translation>(System.Text.Encoding.Default.GetString(buffer));
                _uuid = config.UUID;
                await StartSpeechTranslationEngine(config.LanguageSpoken,
                    config.LanguageTranslated);
                _languageSpoken = config.LanguageSpoken;
                _languageTranslated = config.LanguageTranslated;
                while (!result.CloseStatus.HasValue)
                {

                    byte[] audio;
                    while (_audioToWrite.TryDequeue(out audio))
                    {
                        const int bufferSize = 640;
                        for (var i = 0; i + bufferSize < audio.Length; i += bufferSize)
                        {
                            var audioToSend = audio[i..(i + bufferSize)];
                            var endOfMessage = audio.Length > (bufferSize + i);
                            await webSocket.SendAsync(new ArraySegment<byte>(audioToSend, 0, bufferSize), WebSocketMessageType.Binary, endOfMessage, CancellationToken.None);
                        }
                    }

                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    _inputStream.Write(buffer);
                }
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
            finally
            {
                await StopTranscriptionEngine();
            }
        }

        public void Dispose()
        {
            _inputStream.Dispose();
            _audioInput.Dispose();
            _recognizer.Dispose();
            _synthesizer.Dispose();
            _audioOutputStream.Dispose();
        }
    }
}
