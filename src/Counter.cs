using System;
using System.IO;

using System.Xml.Linq;
using System.Linq;

using System.Diagnostics;

using System.Text.RegularExpressions;

public class CodeCounter
{
    public CodeCounter(string dirName)
    {
        
        try
        {
            // Read in all files in chosen dir
            // while ignoring those that are hidden, or in a hidden dir!
            Stack<string> dirStack = new Stack<string>();
            List<string> fileList = new List<string>();

            // add root for intial search
            dirStack.Push(dirName);

            while(dirStack.Count != 0)
            {
                string curDir = dirStack.Pop();

                // add dirs that aernt hidden for searching
                var dirs = 
                    from file in Directory.GetDirectories(curDir)
                    where !File.GetAttributes(file).HasFlag(FileAttributes.Hidden)
                    select file;

                foreach(string dir in dirs)
                {
                    dirStack.Push(dir);
                }

                // add non hidden files to final list
                var normalFiles =
                    from file in Directory.GetFiles(curDir)
                    where !File.GetAttributes(file).HasFlag(FileAttributes.Hidden)
                    select file;

                fileList.AddRange(normalFiles);
            }

            // filter by a gitignore if present            
            filterGitignore(fileList,dirName);

            // files have been filtered out now they are ready for work
            files = fileList.ToArray();
        }

        catch(Exception ex)
        {
            Console.WriteLine("LOG: {0}",ex.ToString());
            throw new Exception(string.Format("Could not parse dir {0}",dirName));   
        }

        // we need to know how files are going to filtered out first!
        loadConfig();

        // actually perfrom the counting
        countLang();
    }


    void filterGitignore(List<string> fileList, string dirName)
    {
        string ignore = ignore = dirName + "/" + ".gitignore";

        // filter by gitignore if it exists
        if(File.Exists(ignore))
        {
            List<string> filters = File.ReadLines(ignore).ToList();

            for(int i = fileList.Count - 1; i >= 0; i--)
            {
                string file = fileList[i];

                foreach(string filter in filters)
                {
                    // ignore comments in the gitignore
                    if(string.IsNullOrWhiteSpace(filter) || filter.StartsWith("#"))
                    {
                        continue;
                    }


                    bool filtered = false;

                    // convert 'shell' style wildcard filters to regex
                    string reg_filter = Regex.Escape(filter).Replace("\\*",".*");

                    // filter is by dir
                    if(reg_filter.Contains("/"))
                    {
                        // need to apply this relative to the selected dir so filtering is not done
                        // on paths outside the workspace!
                        string relFile = file.Replace(dirName,"");

                        // this filteres at any point in the directory tree
                        filtered = Regex.IsMatch(relFile,".*" + reg_filter);
                    }

                    // filter is by file
                    else
                    {
                        string name = Path.GetFileName(file);
                        filtered = Regex.IsMatch(name,reg_filter);
                    }

                    if(filtered)
                    {
                        //Console.WriteLine("Filtered '{0}' : '{1}'\n",file,filter);
                        fileList.RemoveAt(i);
                        break;
                    }
                }
            }

        }
    }

    void countLang()
    {
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
            
            string lang = "";

            // attempt to match by file name
            if(fileTable.ContainsKey(name))
            {
                lang = fileTable[name];
            }

            // Find what language this exenstion belongs to if any?
            else if(extTable.ContainsKey(ext))
            {
                // accumulate on lang
                lang = extTable[ext];
            }

            // accumulate the count
            if(lang != "")
            {
                langCountTable[lang] += lines;

                // accumulate total
                total += lines;

                // extra match
                match += 1;
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
        Console.WriteLine("total lines: {0} in {1} files\n",total,match);

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

        
        XElement? extConfig = root.Element("lang-ext");
        XElement? fileConfig = root.Element("lang-file");

        // pull extenstion settings
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

        // pull file settings
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
    
    int match = 0;

    // language filtering
    Dictionary<string,string> extTable = new Dictionary<string,string>();
    Dictionary<string,string> fileTable = new Dictionary<string,string>();

    Dictionary<string,int> langCountTable = new Dictionary<string,int>();

}