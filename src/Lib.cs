

public static class Lib
{

    static public bool fileIsPlainText(string filename)
    {
        byte[] bytes = System.IO.File.ReadAllBytes(filename);

        // check if file contains any non printable ascii chars
        foreach(byte v in bytes)
        {
            // TODO: this just ignores any utf-8 chars
            if(v <= 128 && !(v >= 0 && v <= 126))
            {
                return false;
            }
        }

        return true;
    }
}