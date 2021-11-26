﻿//using Catalyst;
//using Catalyst.Models;
//using Mosaik.Core;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Linq;
//using Version = Mosaik.Core.Version;
//using P = Catalyst.PatternUnitPrototype;
//using System.Text;
//using Microsoft.Extensions.Logging;

//namespace Catalyst.Samples.EntityRecognition
//{
//    public static class Program
//    {

//        private static async Task Main()
//        {
//            //Initialize the English built-in models
//            Catalyst.Models.English.Register();

//            Console.OutputEncoding = Encoding.UTF8;
//            //ApplicationLogging.SetLoggerFactory(LoggerFactory.Create(lb => lb.AddConsole()));

//            // Catalyst currently supports 3 different types of models for Named Entity Recognition (NER):
//            // - Gazetteer-like(i.e. [Spotter](https://github.com/curiosity-ai/catalyst/blob/master/Catalyst/src/Models/EntityRecognition/Spotter.cs)) 
//            // - Regex-like(i.e. [PatternSpotter](https://github.com/curiosity-ai/catalyst/blob/master/Catalyst/src/Models/EntityRecognition/PatternSpotter.cs))
//            // - Perceptron (i.e. [AveragePerceptronEntityRecognizer](https://github.com/curiosity-ai/catalyst/blob/master/Catalyst/src/Models/EntityRecognition/AveragePerceptronEntityRecognizer.cs))

//            SpotterSample();

//            await AveragePerceptronEntityRecognizerAndPatternSpotterSample();
//        }

//        private static async Task AveragePerceptronEntityRecognizerAndPatternSpotterSample()
//        {
//            // For training an AveragePerceptronModel, check the source-code here: https://github.com/curiosity-ai/catalyst/blob/master/Catalyst.Training/src/TrainWikiNER.cs
//            // This example uses the pre-trained WikiNER model, trained on the data provided by the paper "Learning multilingual named entity recognition from Wikipedia", Artificial Intelligence 194 (DOI: 10.1016/j.artint.2012.03.006)
//            // The training data was sourced from the following repository: https://github.com/dice-group/FOX/tree/master/input/Wikiner

//            //Configures the model storage to use the online repository backed by the local folder ./catalyst-models/

//            //Create a new pipeline for the english language, and add the WikiNER model to it
//            Console.WriteLine("Loading models... This might take a bit longer the first time you run this sample, as the models have to be downloaded from the online repository");
//            var nlp = await Pipeline.ForAsync(Language.English);
//            nlp.Add(await AveragePerceptronEntityRecognizer.FromStoreAsync(language: Language.English, version: Version.Latest, tag: "WikiNER"));

//            //Another available model for NER is the PatternSpotter, which is the conceptual equivalent of a RegEx on raw text, but operating on the tokenized form off the text.
//            //Adds a custom pattern spotter for the pattern: single("is" / VERB) + multiple(NOUN/AUX/PROPN/AUX/DET/ADJ)
//            var isApattern = new PatternSpotter(Language.English, 0, tag: "is-a-pattern", captureTag: "IsA");
//            isApattern.NewPattern(
//                "Is+Noun",
//                mp => mp.Add(
//                    new PatternUnit(P.Single().WithToken("is").WithPOS(PartOfSpeech.VERB)),
//                    new PatternUnit(P.Multiple().WithPOS(PartOfSpeech.NOUN, PartOfSpeech.PROPN, PartOfSpeech.AUX, PartOfSpeech.DET, PartOfSpeech.ADJ))
//            ));
//            nlp.Add(isApattern);

//            //For processing a single document, you can call nlp.ProcessSingle
//            var doc = new Document(Data.Sample_1, Language.English);
//            nlp.ProcessSingle(doc);

//            //For processing a multiple documents in parallel (i.e. multithreading), you can call nlp.Process on an IEnumerable<IDocument> enumerable
//            var docs = nlp.Process(MultipleDocuments());

//            //This will print all recognized entities. You can also see how the WikiNER model makes a mistake on recognizing Amazon as a location on Data.Sample_1
//            PrintDocumentEntities(doc);
//            foreach (var d in docs) { PrintDocumentEntities(d); }

//            //For correcting Entity Recognition mistakes, you can use the Neuralyzer class. 
//            //This class uses the Pattern Matching entity recognition class to perform "forget-entity" and "add-entity" 
//            //passes on the document, after it has been processed by all other proceses in the NLP pipeline
//            var neuralizer = new Neuralyzer(Language.English, 0, "WikiNER-sample-fixes");

//            //Teach the Neuralyzer class to forget the match for a single token "Amazon" with entity type "Location"
//            neuralizer.TeachForgetPattern("Location", "Amazon", mp => mp.Add(new PatternUnit(P.Single().WithToken("Amazon").WithEntityType("Location"))));

//            //Teach the Neuralyzer class to add the entity type Organization for a match for the single token "Amazon"
//            neuralizer.TeachAddPattern("Organization", "Amazon", mp => mp.Add(new PatternUnit(P.Single().WithToken("Amazon"))));

//            //Add the Neuralyzer to the pipeline
//            nlp.UseNeuralyzer(neuralizer);

//            //Now you can see that "Amazon" is correctly recognized as the entity type "Organization"
//            var doc2 = new Document(Data.Sample_1, Language.English);
//            nlp.ProcessSingle(doc2);
//            PrintDocumentEntities(doc2);
//        }

//        private static void SpotterSample()
//        {
//            //Another way to perform entity recognition is to use a gazeteer-like model. For example, here is one for capturing a set of programing languages
//            var spotter = new Spotter(Language.Any, 0, "programming", "ProgrammingLanguage");
//            spotter.Data.IgnoreCase = true; //In some cases, it might be better to set it to false, and only add upper/lower-case exceptions as required

//            spotter.AddEntry("apple");
//            spotter.AddEntry("Python");
//            spotter.AddEntry("Python 3"); //entries can have more than one word, and will be automatically tokenized on whitespace
//            spotter.AddEntry("C++");
//            spotter.AddEntry("Rust");
//            spotter.AddEntry("Java");

//            var nlp = Pipeline.TokenizerFor(Language.English);
//            nlp.Add(spotter); //When adding a spotter model, the model propagates any exceptions on tokenization to the pipeline's tokenizer

//            var docAboutProgramming = new Document(Data.SampleProgramming, Language.English);

//            nlp.ProcessSingle(docAboutProgramming);

//            PrintDocumentEntities(docAboutProgramming);
//        }

//        private static void PrintDocumentEntities(IDocument doc)
//        {
//            Console.WriteLine($"Input text:\n\t'{doc.Value}'\n\nTokenized Value:\n\t'{doc.TokenizedValue(mergeEntities: true)}'\n\nEntities: \n{string.Join("\n", doc.SelectMany(span => span.GetEntities()).Select(e => $"\t{e.Value} [{e.EntityType.Type}]"))}");
//        }

//        static IEnumerable<IDocument> MultipleDocuments()
//        {
//            yield return new Document(Data.Sample_2, Language.English);
//            yield return new Document(Data.Sample_3, Language.English);
//            yield return new Document(Data.Sample_4, Language.English);
//        }
//    }

//    public static class Data
//    {
//        public const string AppleAmber = @"APPLE AMBER
//Serves 4

//Ingredients 
//750g cooking apples 
//100g caster sugar  
//2 eggs

//ovenproof dish

//Method
//1.	Prepare an oven, Gas 3 or 170°C.Grease the sides of the oven proof dish.
//2.	Peel and slice the apples.Put into a medium saucepan with 2 tbsp of water and cook over low heat until soft.   
//3.	Separate the eggs (break the egg onto a saucer and place an egg cup over the yolk, pour the white into a large bowl).    
//4.	Add the egg yolks to the cooked apple and stir well.
//5.	Put the apple into the oven proof dish.
//6.	Whisk egg whites until stiff.Add half the sugar and whisk again until mixture stands in peaks.
//7.	Fold in the rest of the sugar and spoon or pipe onto the apple.  
//8.	Place in oven and bake for 30 mins.until golden brown.

//Note: 
//•	it is important to make sure that the meringue is well baked to kill salmonella bacteria.  If possible check with a temperature probe.
//•	separate eggs carefully because the slightest trace of yolk will prevent the egg whites from whisking. 

//Variations:
//•	use other varieties of fruit, for example rhubarb or plums.
//•	could be cooked in a pastry case.
 
//";
//        public const string Sample_1 = "750g cooking apples ";
//        public const string Sample_2 = "Berlin is the capital and largest city of Germany by both area and population. Its 3,748,148 (2018) inhabitants make it the second most populous city proper of the European Union after London.";
//        public const string Sample_3 = "Microsoft is an American multinational technology company with headquarters in Redmond, Washington.";
//        public const string Sample_4 = "Munich is the capital and most populous city of Bavaria, the second most populous German federal state.";
//        public const string SampleProgramming = "750g cooking apples  Being the descendant of C and with its code compiled, C++ excels such languages as Python, C#, or any interpreted language. In terms of Rust vs. C++, Rust is frequently proclaimed to be faster than C++ due to its unique components.";
//    }
//}