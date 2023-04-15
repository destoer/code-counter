using System;
using System.IO;

using System.Xml.Linq;
using System.Linq;


public class CodeCounter
{
    public CodeCounter(string dirName)
    {
        // Read in all files in chosen dir
        try
        {
            files = Directory.EnumerateFiles(dirName,"*",SearchOption.AllDirectories).ToArray();
        }

        catch
        {
            throw new Exception(string.Format("Could not parse dir {0}",dirName));   
        }

        // we need to know how files are going to filtered out first!
        loadConfig();

        foreach(string file in files)
        {
            string ext = Path.GetExtension(file);

            // dont count files that are binary or have no ext
            if(!Lib.fileIsPlainText(file) || ext == "")
            {
                //Console.WriteLine("Filtered: {0}",file);
                continue;
            }

            int lines = countLines(file);

            // What language does this file belong to?
            // TODO: at the moment this is really simple and works on extension alone
            // and cannot resolve stuff like wether or .h file is C++ or C code
            
            // Find what language this exenstion belongs to if any?
            if(extTable.ContainsKey(ext))
            {
                // accumulate on lang
                string lang = extTable[ext];

                langCountTable[lang] += lines;

                // accumulate total
                total += lines;
            }

            else
            {
                // do the default
                other += lines;
            }
        }
    }

    // TODO(destoer): for now this is command line only!
    // we want a pretty graph printer later
    public void printCount()
    {
        Console.WriteLine("total lines: {0}\n",total);

        // Lines with percentage of total
        foreach(var item in langCountTable)
        {
            int count = item.Value;

            if(count == 0)
            {
                continue;
            }

            Console.WriteLine("{0}: {1} {2}%",item.Key,count,100.0 * ((float)count / (float)total));
        }

        if(other != 0)
        {
            Console.WriteLine("\nother: {0}",other);
        }
    }

    // TODO: should we allow for specifying what config to load?
    void loadConfig()
    {
        XDocument xDoc;
        try
        {
            xDoc = XDocument.Load("config.xml");
        }

        catch
        {
            throw new Exception("Could not parse configuration file config.xml");
        }

        // Pull extenstion settings   
        XElement extConfig = xDoc.Element("ext-config") ??  throw new Exception("Could not parse configuration file config.xml");


        // for each ext-config rip the extenstion and lang
        foreach(XElement xe in extConfig.Elements())
        {
            ArgumentNullException.ThrowIfNull(xe,"Could not parse configuration file config.xml");

            string ext = xe.Attribute("ext")?.Value ?? throw new Exception("Could not parse configuration file config.xml");
            string lang = xe.Attribute("lang")?.Value ?? throw new Exception("Could not parse configuration file config.xml");

            extTable[ext] = lang;
            langCountTable[lang] = 0;
        }
    }

    int countLines(string file)
    {
        int count = File.ReadLines(file).Count();
        return count;
    }

    string[] files;

    // total lines of source code
    int total = 0;

    // number of lines not picked up by filtering
    int other = 0;

    // language filtering
    Dictionary<string,string> extTable = new Dictionary<string,string>();
    Dictionary<string,int> langCountTable = new Dictionary<string,int>();

}