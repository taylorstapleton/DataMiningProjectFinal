using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace DataMiningFinalProject
{
    class Program
    {

        #region vars
        public static List<List<Tweet>> Tweets;

        public static int FileNumber = 100;
        
        public static int gramSize = 1;

        public static int numCounters = 10;

        public static ConcurrentDictionary<string, List<Slice>> wordToSlice = new ConcurrentDictionary<string, List<Slice>>();

        public static ConcurrentDictionary<Slice, List<Counter>> sliceToWord = new ConcurrentDictionary<Slice, List<Counter>>();

        #endregion

        #region main
        static void Main(string[] args)
        {
            Tweets = new List<List<Tweet>>();
            
            loadTweets(FileNumber);

            MGDriver();

            Console.Read();
        }

        #endregion

        #region driver

        public static void MGDriver()
        {
            List<Counter>[,] slices = new List<Counter>[62, 20];

            for(int i = 0; i < 62; i++)
            {
                for(int j = 0; j<20; j++)
                {
                    slices[i, j] = new List<Counter>();
                }
            }

            int fileCount = 0;
            foreach(var list in Tweets)
            {
                Console.WriteLine("fileCount = " + fileCount++);
                foreach (var tweet in list)
                {
                    var decodedText = HttpUtility.UrlDecode(tweet.Text);

                    var grams = createGrams(decodedText, gramSize);

                    int slicedLat = (int)tweet.Latitude - 30;
                    int slicedLong = (int)tweet.Longitude + 125;

                    var currentSlice = slices[slicedLong, slicedLat];

                    MG(grams, ref currentSlice);

                }
            }

            int latSlice = 0;
            for (int i = 0; i < 62; i++)
            {
                Console.WriteLine("latSlice = " + latSlice++);
                for (int j = 0; j < 20; j++)
                {
                    var currentSlice = slices[i, j];

                    foreach (var counter in currentSlice)
                    {
                        wordToSlice.AddOrUpdate(counter.gram,
                            new List<Slice> { new Slice() { latitude = j, longitude = i } },
                            (oldKey, oldValue) => new List<Slice>(oldValue) { new Slice() { latitude = j, longitude = i } });

                    }

                    sliceToWord.TryAdd(new Slice() { latitude = j, longitude = i },
                        (from counter in currentSlice select counter).ToList());
                }
            }

        }

        #endregion

        #region MGAlg

        public static void MG(List<string> elements, ref List<Counter> counters)
        {
            foreach (var element in elements)
            {
                var existingCounter = (from counter in counters where counter.gram == element select counter);
                if (existingCounter.Any())
                {
                    existingCounter.First().count++;
                    continue;
                }
                else
                {
                    if (counters.Count < numCounters)
                    {
                        counters.Add(new Counter() { gram = element, count = 1 });
                    }
                    else
                    {
                        foreach (var counter in counters)
                        {
                            counter.count--;
                        }
                    }
                }

                counters.RemoveAll(c => c.count <= 0);
            }
        }

        #endregion

        #region createGrams

        public static List<string> createGrams(string toCreate, int size)
        {
            var splits = toCreate.Split(' ');
            var toReturn = new List<string>();

            for(int i = 0; i < splits.Length - size; i++)
            {
                List<string> current = new List<string>();
                for(int j =0; j<size; j++)
                {
                    current.Add(splits[i + j]);
                }
                toReturn.Add(string.Join(" ", current.ToArray()));

            }

            return toReturn;
        }

        #endregion

        #region load tweets
        public static void loadTweets(int numberOfFiles)
        {
            foreach(var i in Enumerable.Range(0, numberOfFiles))
            {
                List<string> lines = File.ReadAllLines(@"C:\Users\taylor\Desktop\TweetData\Sunday22\TweetFile" + i + ".tweet").ToList();

                var tweets = new List<Tweet>();
                try
                {
                    //tweets = (from line in lines select JsonConvert.DeserializeObject<Tweet>(line)).ToList();

                    foreach(var line in lines)
                    {
                        var current = JsonConvert.DeserializeObject<Tweet>(line);

                        current.Text = HttpUtility.UrlDecode(current.Text);

                        current.Text = string.Join(" ", (from word in current.Text.Split(new string[] { " ", "  ", "   ", "    " }, StringSplitOptions.None) where word.StartsWith("#") select word.Trim()));

                        if(!String.IsNullOrWhiteSpace(current.Text))
                        {
                            tweets.Add(current);
                        }
                    }

                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                Tweets.Add(tweets);
                Console.WriteLine(i);
            }
        }

        #endregion
    }
}
