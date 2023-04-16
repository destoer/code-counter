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
            // TODO: we need a more in depth method to ignore hidden files and the gitignore

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
            string name = Path.GetFileName(file);
            string ext = Path.GetExtension(name);

            // dont count files that are binary or have no ext
            if(!Lib.fileIsPlainText(file) || ext == "")
            {
                //Console.WriteLine("Filtered: {0}",file);
                continue;
            }

            int lines = countLines(file);

            // What language does this file belong to?
            // TODO: at the moment this is really simple and works on name extension alone
            // and cannot resolve stuff like wether or .h file is C++ or C code
            
            // attempt to match by file name
            if(fileTable.ContainsKey(name))
            {
                string lang = fileTable[name];

                langCountTable[lang] += lines;

                // accumulate total
                total += lines;
            }


            // Find what language this exenstion belongs to if any?
            else if(extTable.ContainsKey(ext))
            {
                // accumulate on lang
                string lang = extTable[ext];

                langCountTable[lang] += lines;

                // accumulate total
                total += lines;
            }

            // unknown kind of file
            else
            {
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

        catch(Exception ex)
        {
            Console.WriteLine("Log: {0}",ex.Message);
            throw new Exception("Could not load configuration file config.xml");
        }

        XElement root = xDoc.Element("config") ?? throw new Exception("Could not find root onfig");

        // Pull extenstion settings   
        XElement? extConfig = root.Element("lang-ext");
        XElement? fileConfig = root.Element("lang-file");

        if(extConfig != null)
        {
            // for each ext config rip the extenstion and lang
            foreach(XElement xe in extConfig.Elements())
            {
                ArgumentNullException.ThrowIfNull(xe,"Could not parse extenstion entry");

                string ext = xe.Attribute("ext")?.Value ?? throw new Exception("Could not parse exenstion entry (ext)");
                string lang = xe.Attribute("lang")?.Value ?? throw new Exception("Could not parse exenstion entry (lang)");

                extTable[ext] = lang;
                langCountTable[lang] = 0;
            }
        }


        if(fileConfig != null)
        {
            // for each file config rip filename and lang
            foreach(XElement xe in fileConfig.Elements())
            {
                ArgumentNullException.ThrowIfNull(xe,"Could not parse extenstion entry");

                string file = xe.Attribute("name")?.Value ?? throw new Exception("Could not parse file entry (name)");
                string lang = xe.Attribute("lang")?.Value ?? throw new Exception("Could not parse file entry (lang)");

                fileTable[file] = lang;
                langCountTable[lang] = 0;                
            }

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
    Dictionary<string,string> fileTable = new Dictionary<string,string>();

    Dictionary<string,int> langCountTable = new Dictionary<string,int>();

}