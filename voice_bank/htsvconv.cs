using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class HTSVoiceConverter
{
    // global const
    private static string[] stream_type = { "MCP", "LF0", "LPF" };
    private static string[] stream_name = { "mgc", "lf0", "lpf" };
    private static string fullcontext_format = "HTS_TTS_JPN";
    private static string hts_voice_version = "1.0";
    private static string fullcontext_version = "1.0";

    // var
    private static string sampling_frequency = "48000";
    private static string frame_period = "240";
    private static string alpha = "0.55";
    private static string gv_off_context = "\"*-sil+*\",\"*-pau+*\"";

    // temp file name
    private static string temp_text = "_temp.txt";
    private static string temp_data = "_temp.dat";
    private static string curdir;

    private static int countTree(string target)
    {
        int counter = 0;
        string line;

        string path = Path.Combine(curdir, target);

        if (File.Exists(path))
        {
            StreamReader sr = new StreamReader(path);
            while ((line = sr.ReadLine()) != null)
            {
                Regex regex = new Regex(@".*\[(.+)\]$");
                Match m = regex.Match(line);
                if (m.Success)
                {
                    counter++;
                }
            }
        }
        else
        {
            Console.Error.WriteLine("error : not found : {0}", path);
            throw new Exception();
        }

        return counter;
    }

    private struct Pdf
    {
        public int is_msd;
        public int ntree;
        public int stream_size;
        public int vector_length;
        public int[] npdf;
        public List<float> data;
    }

    private struct Range
    {
        public int s, e;
    }

    private static int bigendian_int(byte[] bytes)
    {
        if (bytes.Length == sizeof(int))
        {
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
        else
        {
            return 0;
        }
    }

    private static Pdf parsePdf(string name_pdf, string name_tree, int num_windows)
    {
        string file_pdf = Path.Combine(curdir, name_pdf);
        string file_tree = Path.Combine(curdir, name_tree);

        if (!File.Exists(file_pdf))
        {
            Console.Error.WriteLine("error : not found : {0}", file_pdf);
            throw new Exception();
        }
        if (!File.Exists(file_tree))
        {
            Console.Error.WriteLine("error : not found : {0}", file_tree);
            throw new Exception();
        }

        Pdf result = new Pdf();
        byte[] byteArray;
        result.ntree = countTree(name_tree);

        BinaryReader binReader = new BinaryReader(File.Open(file_pdf, FileMode.Open));
        result.is_msd = bigendian_int(binReader.ReadBytes(sizeof(int)));

        result.stream_size = bigendian_int(binReader.ReadBytes(sizeof(int)));
        result.vector_length = bigendian_int(binReader.ReadBytes(sizeof(int)));

        result.npdf = new int[result.ntree];
        for (int i = 0; i < result.ntree; i++)
        {
            result.npdf[i] = bigendian_int(binReader.ReadBytes(sizeof(int)));
        }

        List<float> _data = new List<float>();
        while (true)
        {
            byteArray = binReader.ReadBytes(sizeof(float));
            if (byteArray.Length != sizeof(float))
            {
                break;
            }
            Array.Reverse(byteArray);
            _data.Add(BitConverter.ToSingle(byteArray, 0));
        }
        binReader.Close();

        Console.WriteLine(name_pdf + ":");
        Console.WriteLine("num_windows  = {0}", num_windows);
        Console.WriteLine("ntree        = {0}", result.ntree);
        Console.WriteLine("is_msd       = {0}", result.is_msd);
        Console.WriteLine("stream_size  = {0}", result.stream_size);
        Console.WriteLine("vector_length= {0}", result.vector_length);
        string[] str_npdf = new string[result.npdf.Length];
        for (int i = 0; i < result.npdf.Length; i++)
        {
            str_npdf[i] = result.npdf[i].ToString();
        }
        Console.WriteLine("npdf         = {0}", "{ " + string.Join(",", str_npdf) + " }");
        Console.WriteLine();

        int dp = 0;
        float[] pdf;

        result.data = new List<float>();
        try
        {
            if (result.is_msd == 1)
            {
                for (int j = 0; j < result.ntree; j++)
                {
                    for (int k = 0; k < result.npdf[j]; k++)
                    {
                        pdf = new float[result.vector_length * 2 + 1];
                        for (int l = 0; l < result.stream_size; l++)
                        {
                            for (int m = 0; m < result.vector_length / result.stream_size; m++)
                            {
                                pdf[l * result.vector_length / result.stream_size + m] = _data[dp++];
                                pdf[l * result.vector_length / result.stream_size + m + result.vector_length] = _data[dp++];
                            }
                            float temp = _data[dp++];
                            if (l == 0)
                            {
                                pdf[2 * result.vector_length] = temp;
                            }
                            temp = _data[dp++];
                        }
                        result.data.AddRange(pdf);
                    }
                }
            }
            else
            {
                for (int j = 0; j < result.ntree; j++)
                {
                    for (int k = 0; k < result.npdf[j]; k++)
                    {
                        pdf = new float[result.vector_length * 2];
                        for (int l = 0; l < result.vector_length; l++)
                        {
                            pdf[l] = _data[dp++];
                            pdf[l + result.vector_length] = _data[dp++];
                        }
                        result.data.AddRange(pdf);
                    }
                }
            }
        }
        catch (Exception)
        {
            Console.Error.WriteLine("error : illegal data in {0}", name_pdf);
            throw;
        }
        result.vector_length /= num_windows;

        return result;
    }


    private static string getTreePath(string basename)
    {
        string path = Path.Combine(curdir, "tree-" + basename + ".inf");
        if (!File.Exists(path))
        {
            Console.Error.WriteLine("error : not found : {0}", path);
            throw new Exception();
        }
        return path;
    }

    private static string getWinPath(string basename, int num)
    {
        string path = Path.Combine(curdir, basename + ".win" + (num + 1));
        if (!File.Exists(path))
        {
            Console.Error.WriteLine("error : not found : {0}", path);
            throw new Exception();
        }
        return path;
    }


    private static void generate(string dir, string name_htsvoice)
    {
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine("error : no directory : {0}", dir);
            throw new Exception();
        }
        curdir = dir;
        Console.WriteLine(name_htsvoice);

        int num_streams = stream_type.Length;

        Pdf[] stream_pdf = new Pdf[num_streams];
        string[] option = new string[num_streams];
        option[0] = "ALPHA=" + alpha;

        int[] num_windows = new int[num_streams];
        int max_num_windows = 0;
        for (int i = 0; i < num_streams; i++)
        {
            int j = 0;
            string temp_name;
            do
            {
                j++;
                temp_name = Path.Combine(curdir, stream_name[i] + ".win" + j);
            } while (File.Exists(temp_name));
            num_windows[i] = --j;
            if (max_num_windows < j)
            {
                max_num_windows = j;
            }
        }
        int[] use_gv = new int[num_streams];
        for (int i = 0; i < num_streams; i++)
        {
            use_gv[i] = (File.Exists(Path.Combine(curdir, "gv-" + stream_name[i] + ".pdf"))) ? 1 : 0;
        }

        /*** parse config.txt ***/
        string target_name = "config.txt";
        List<string> option_temp = new List<string>();
        if (File.Exists(target_name))
        {
            string line;
            StreamReader sr = new StreamReader(target_name);
            while ((line = sr.ReadLine()) != null)
            {
                Regex kandv = new Regex("(.+)=(.+)");
                Match m = kandv.Match(line);
                if (m.Success)
                {
                    if (m.Groups[1].Value.ToLower() == "s")
                    {
                        sampling_frequency = m.Groups[2].Value;
                        Console.WriteLine("sampling_frequency = {0}", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value.ToLower() == "sampling_frequency")
                    {
                        sampling_frequency = m.Groups[2].Value;
                        Console.WriteLine("sampling_frequency = {0}", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value.ToLower() == "p")
                    {
                        frame_period = m.Groups[2].Value;
                        Console.WriteLine("frame_period = {0}", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value.ToLower() == "frame_period")
                    {
                        frame_period = m.Groups[2].Value;
                        Console.WriteLine("frame_period = {0}", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value.ToLower() == "a")
                    {
                        option_temp.Add("ALPHA=" + m.Groups[2].Value);
                        Console.WriteLine("alpha = {0}", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value.ToLower() == "alpha")
                    {
                        option_temp.Add("ALPHA=" + m.Groups[2].Value);
                        Console.WriteLine("alpha = {0}", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value.ToLower() == "gamma")
                    {
                        option_temp.Add("GAMMA=" + m.Groups[2].Value);
                        Console.WriteLine("gamma = {0}", m.Groups[2].Value);
                    }
                    else if (m.Groups[1].Value.ToLower() == "ln_gain")
                    {
                        option_temp.Add("LN_GAIN=" + m.Groups[2].Value);
                        Console.WriteLine("ln_gain = {0}", m.Groups[2].Value);
                    }
                }
            }
        }
        if ( option_temp.Count>0 )
        {
	        string[] option_temp2 = new string[option_temp.Count];
	        option_temp.CopyTo(option_temp2);
	        option[0] = string.Join(" ", option_temp2);
	    }
        Console.WriteLine();

        /*** parse gv-switch.inf ***/
        string file_gv_switch = Path.Combine(curdir, "gv-switch.inf");
        if (File.Exists(file_gv_switch))
        {
            StreamReader sr_gv_switch = new StreamReader(file_gv_switch);
            string text = sr_gv_switch.ReadToEnd();
            sr_gv_switch.Close();
            text = Regex.Replace(text, "[ \t\n\r]+", " ", RegexOptions.Singleline);
            Regex regex1 = new Regex(@"QS gv\-switch \{ *([^\}]+) *\}");
            Match m1 = regex1.Match(text);
            if (m1.Success)
            {
                gv_off_context = Regex.Replace(m1.Groups[1].Value, " *$", "");
            }
        }

        /*** make bin file ***/
        int pos = 0;
        Range range_dur;
        Range range_dur_tree;
        Range[,] range_win = new Range[num_streams, max_num_windows];
        Range[] range_pdf = new Range[num_streams];
        Range[] range_tree = new Range[num_streams];
        Range[] range_gv_pdf = new Range[num_streams];
        Range[] range_gv_tree = new Range[num_streams];

        FileStream fw = new FileStream(temp_data, FileMode.Create);
        try
        {
            BinaryWriter bw = new BinaryWriter(fw);
            Pdf target_pdf;
            string target;
            byte[] bytes;

            // dur
            target = "dur";
            target_pdf = parsePdf(target + ".pdf", "tree-" + target + ".inf", 1);
            foreach (int d in target_pdf.npdf)
            {
                bw.Write(d);
            }
            foreach (float f in target_pdf.data)
            {
                bw.Write(f);
            }
            bytes = File.ReadAllBytes(getTreePath(target));
            bw.Write(bytes);
            range_dur.s = pos;
            pos += target_pdf.ntree * sizeof(int);
            pos += target_pdf.data.Count * sizeof(float);
            range_dur.e = pos - 1;
            range_dur_tree.s = pos;
            pos += bytes.Length;
            range_dur_tree.e = pos - 1;

            // win
            for (int i = 0; i < num_streams; i++)
            {
                for (int j = 0; j < num_windows[i]; j++)
                {
                    bytes = File.ReadAllBytes(getWinPath(stream_name[i], j));
                    bw.Write(bytes);
                    range_win[i, j].s = pos;
                    pos += bytes.Length;
                    range_win[i, j].e = pos - 1;
                }
            }

            // stream
            for (int i = 0; i < num_streams; i++)
            {
                target = stream_name[i];
                stream_pdf[i] = parsePdf(target + ".pdf", "tree-" + target + ".inf", num_windows[i]);
                foreach (int d in stream_pdf[i].npdf)
                {
                    bw.Write(d);
                }
                foreach (float f in stream_pdf[i].data)
                {
                    bw.Write(f);
                }
                range_pdf[i].s = pos;
                pos += stream_pdf[i].ntree * sizeof(int);
                pos += stream_pdf[i].data.Count * sizeof(float);
                range_pdf[i].e = pos - 1;
            }
            for (int i = 0; i < num_streams; i++)
            {
                bytes = File.ReadAllBytes(getTreePath(stream_name[i]));
                bw.Write(bytes);
                range_tree[i].s = pos;
                pos += bytes.Length;
                range_tree[i].e = pos - 1;
            }

            // gv
            for (int i = 0; i < num_streams; i++)
            {
                if (use_gv[i] != 0)
                {
                    target = "gv-" + stream_name[i];
                    target_pdf = parsePdf(target + ".pdf", "tree-" + target + ".inf", 1);
                    foreach (int d in target_pdf.npdf)
                    {
                        bw.Write(d);
                    }
                    foreach (float f in target_pdf.data)
                    {
                        bw.Write(f);
                    }
                    range_gv_pdf[i].s = pos;
                    pos += target_pdf.ntree * sizeof(int);
                    pos += target_pdf.data.Count * sizeof(float);
                    range_gv_pdf[i].e = pos - 1;
                }
            }
            for (int i = 0; i < num_streams; i++)
            {
                if (use_gv[i] != 0)
                {
                    bytes = File.ReadAllBytes(getTreePath("gv-" + stream_name[i]));
                    bw.Write(bytes);
                    range_gv_tree[i].s = pos;
                    pos += bytes.Length;
                    range_gv_tree[i].e = pos - 1;
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.Error.Write("Error : File Not Found");
            throw;
        }
        catch (Exception)
        {
            fw.Close();
            if ( File.Exists(temp_data) )
            {
                File.Delete(temp_data);
            }
            throw;
        }
        fw.Close();

        /**** make text file ****/
        StreamWriter sw = new StreamWriter(temp_text, false);
        sw.Write("[GLOBAL]\n");
        sw.Write("HTS_VOICE_VERSION:" + hts_voice_version + "\n");
        sw.Write("SAMPLING_FREQUENCY:" + sampling_frequency + "\n");
        sw.Write("FRAME_PERIOD:" + frame_period + "\n");
        sw.Write("NUM_STATES:" + countTree("tree-" + stream_name[0] + ".inf") + "\n");
        sw.Write("NUM_STREAMS:" + num_streams + "\n");
        sw.Write("STREAM_TYPE:" + string.Join(",", stream_type) + "\n");
        sw.Write("FULLCONTEXT_FORMAT:" + fullcontext_format + "\n");
        sw.Write("FULLCONTEXT_VERSION:" + fullcontext_version + "\n");
        sw.Write("GV_OFF_CONTEXT:" + gv_off_context + "\n");
        sw.Write("COMMENT:\n");
        sw.Write("[STREAM]\n");
        for (int i = 0; i < num_streams; i++)
        {
            sw.Write("VECTOR_LENGTH[" + stream_type[i] + "]:" + stream_pdf[i].vector_length + "\n");
        }
        for (int i = 0; i < num_streams; i++)
        {
            sw.Write("IS_MSD[" + stream_type[i] + "]:" + stream_pdf[i].is_msd + "\n");
        }
        for (int i = 0; i < num_streams; i++)
        {
            sw.Write("NUM_WINDOWS[" + stream_type[i] + "]:" + num_windows[i] + "\n");
        }
        for (int i = 0; i < num_streams; i++)
        {
            sw.Write("USE_GV[" + stream_type[i] + "]:" + use_gv[i] + "\n");
        }
        for (int i = 0; i < num_streams; i++)
        {
            sw.Write("OPTION[" + stream_type[i] + "]:" + option[i] + "\n");
        }
        sw.Write("[POSITION]" + "\n");
        sw.Write("DURATION_PDF:" + range_dur.s + "-" + range_dur.e + "\n");
        sw.Write("DURATION_TREE:" + range_dur_tree.s + "-" + range_dur_tree.e + "\n");
        for (int i = 0; i < num_streams; i++)
        {
            string[] temp_win = new string[num_windows[i]];
            for (int j = 0; j < num_windows[i]; j++)
            {
                temp_win[j] = "" + range_win[i, j].s + "-" + range_win[i, j].e;
            }
            sw.Write("STREAM_WIN[" + stream_type[i] + "]:" + string.Join(",", temp_win) + "\n");
        }
        for (int i = 0; i < num_streams; i++)
        {
            sw.Write("STREAM_PDF[" + stream_type[i] + "]:" + range_pdf[i].s + "-" + range_pdf[i].e + "\n");
        }
        for (int i = 0; i < num_streams; i++)
        {
            sw.Write("STREAM_TREE[" + stream_type[i] + "]:" + range_tree[i].s + "-" + range_tree[i].e + "\n");
        }
        for (int i = 0; i < num_streams; i++)
        {
            if (use_gv[i] != 0)
            {
                sw.Write("GV_PDF[" + stream_type[i] + "]:" + range_gv_pdf[i].s + "-" + range_gv_pdf[i].e + "\n");
            }
        }
        for (int i = 0; i < num_streams; i++)
        {
            if (use_gv[i] != 0)
            {
                sw.Write("GV_TREE[" + stream_type[i] + "]:" + range_gv_tree[i].s + "-" + range_gv_tree[i].e + "\n");
            }
        }
        sw.Write("[DATA]" + "\n");
        sw.Close();

        /*** unify files ***/
        FileStream fw2 = new FileStream(name_htsvoice, FileMode.Create);
        BinaryWriter bw2 = new BinaryWriter(fw2);
        byte[] bindata = File.ReadAllBytes(temp_text);
        bw2.Write(bindata);
        bindata = File.ReadAllBytes(temp_data);
        bw2.Write(bindata);
        fw2.Close();
        File.Delete(temp_text);
        File.Delete(temp_data);
        Console.WriteLine(name_htsvoice);
    }


    public static void Main(string[] args)
    {
        string name;
        try
        {
            if (args.Length == 0)
            {
                name = Path.GetFileName(Directory.GetCurrentDirectory()) + ".htsvoice";
                generate(Directory.GetCurrentDirectory(), name);
            }
            else
            {
                foreach (string arg in args)
                {
                    if (Directory.Exists(arg))
                    {
                        name = Path.GetFileName(arg) + ".htsvoice";
                        generate(arg, name);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("Exception Handler: {0}", e.ToString());
        }
        return;
    }
}
