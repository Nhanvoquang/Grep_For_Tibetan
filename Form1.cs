using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Text.RegularExpressions;

using System.Windows.Input;

namespace Grep_For_Tibetan
{
   
    public partial class Form1 : Form
    {
        public object MouseButtonState { get; private set; }

        static class Global
        {
            public static int SLength = 28;  //
            public static string SToken = "\n";
            //range of around bytes
            public static int SMin = 1;
            public static int CodingPrfx = 12; // "System.Text."
            public static int TibetanCheckLength = 512;  // check if it is Tibetan
            public static Encoding Ftype = Encoding.UTF8;
            public static string[] CTibetan = { "་","།" };
            public static byte[] tseg = { 0xE0, 0xBC, 0x8D };   //"།"
            public static byte[] tshig = { 0xE0, 0xBC, 0x8D };   // "་"
            public static byte[] Utseg = { 0x0D, 0x0F };        //"།"
            public static byte[] Utshig = { 0x0B, 0x0F };

            //public static byte[,] TibId = new byte[,] {{ 0xE0, 0xBC, 0x8B }, { 0xE0, 0xBC, 0x8D }};  // tseg "་",  and tshig grub "།" 
            public static string[] IncludeExt = { ".txt", ".dat", ".tib", ".dict" };

            //public static List<string> SFound = new List<string>();
            //public static Dictionary<string, string> SFound = new Dictionary<string, string>();
            public static int MaximumFoundItems = 200;
            public static int CurrentDoneItems = 0;
            public static int MaxSearchingItems = 10240;
            public static int TotalMustSearchItems = 0;
            //3Mb Max File size limitation
            public static long MaxFSize = 3072000;
            public static long MaxKangTengBuff = 3072000 * 2; //6MB
            public static List<long> lFileSizes = new List<long>();

            //key data: KeyAnd is the major first to find
            public static List<string> KeyAnds = new List<string>();
            //public static string[] KeyAnd = new string[] { };
            //KeyOrs is the rule after KeyAnd then to qualify within tokens*SLength
            //change to extend support for multiple OR phrases up to 
            public const int MAXOR = 5;
            public static List<string>[] KeyOrsE = new List<string>[MAXOR];
            //public static string[] KeyOrs = new string[] { };
            //KeyExs is the rule after KayAnd then to disqualify within tokens*SLength
            public static List<string> KeyExs = new List<string>();
            //public static string[] KeyExs = new string[] { };
            //KeyReG using for Regex 
            public static List<string> KeyReG = new List<string>();
            //public static string[] KeyReG = new string[] { };
            public static string oldDir = "";
            public static string BegJunk = "";
            public static string EndJunk = "";
            public static string[] SplitToken = { "   ", "\t" , "\n" , "\r" };
            public static string[] SplitTokenE = { "#", "@", "&" };
            public static bool KangTeng =  true;
            public const int LongWord = 4; // evarage word len 3.8 charecters
            public const int stanzasAllowedMax = 4;
            public const int tsheMax = 256;  //phraseMax/4;  "་"  (7++)= number of sound per lines
            public const int tsigMax = 16; //evarage 1 tsig got < 64 letter mean 512/64 = 8
            public const int phraseMax = LongWord * 8 * 4 * stanzasAllowedMax; //wordleng * 7 sound per line * 4 each stanza * 4 stansaz
            public static readonly int[] tokenMax = { tsheMax, tsigMax, phraseMax };
            public static readonly string[] strTokenMax = { "ཙེག་", "ཚིག་གྲུབ་", "Letters" };
            //minimum length of a serch term
            public static int MinSTerm = 8;
            public static int BegEnd;
            public static readonly string[] unwanted = { "\n","\r", "\t"};
            public static string oldfname = "";
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
           
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            listBox1.Enabled = true;

            FBD.SelectedPath = Application.StartupPath; 
            //FBD.SelectedPath = @"F:\CurrentWorking\HuongtichEngagework\syllogism\10 - DHARAMAS";
            //FBD.SelectedPath = @"C:\KangTengPlusName";
            
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                button5.Text = "Hold Until Done Reading...";
                //button5.Enabled = false;
                button5.Refresh();
                int old_count = listBox1.Items.Count;
                DirSearch_ex3(FBD.SelectedPath, listBox1);
                if (old_count == listBox1.Items.Count)
                {
                    MessageBox.Show("No new " + Global.Ftype.ToString().Substring(Global.CodingPrfx) + " Tibetan files found in the selected directory!");
                }
                else
                {
                    listBox1.Sorted = true;
                    label7.Visible = true;
                    label8.Visible = true;
                }
            }
            if (listBox1.Items.Count > 0)
            {
                button5.Enabled = true;
            }
            button5.Text = "Search";
            button5.Enabled = true;
        }

        static void DirSearch_ex3(string sDir, ListBox lbox)
        {
            try
            {
                foreach (string f in Directory.GetFiles(sDir))
                {
                    if ((!lbox.Items.Contains(f)) && (GetEncoding(f)==Global.Ftype))
                    {
                        FileInfo fi = new FileInfo(f);
                        Global.lFileSizes.Add(fi.Length);                            
                        lbox.Items.Add(f);
                    }
                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    DirSearch_ex3(d, lbox);
                }
            }
            catch (System.Exception excpt)
            {
                MessageBox.Show(excpt.ToString());
            }
        }

        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[Global.TibetanCheckLength];

            string ext = Path.GetExtension(filename).ToLower();
            if (ext.Length == 0 || Global.IncludeExt.Contains(ext))
            {
                using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    file.Read(bom, 0, Global.TibetanCheckLength);
                }
                if (Global.Ftype == Encoding.UTF8)
                {
                    if (isSubArray(bom, Global.TibetanCheckLength, Global.tseg, Global.tseg.Length))
                    {
                        return Encoding.UTF8;
                    }
                    if (isSubArray(bom, Global.TibetanCheckLength, Global.tshig, Global.tshig.Length))
                    {
                        return Encoding.UTF8;
                    }
                }
                else  //Unicode
                {
                    if ((bom[0] == 0xff && bom[1] == 0xfe) || (Global.Ftype == Encoding.Unicode))  // If Unicode format
                    {
                        if (isSubArray(bom, Global.TibetanCheckLength , Global.Utseg, Global.Utseg.Length))
                        {
                            return Encoding.Unicode;
                        }
                        if (isSubArray(bom, Global.TibetanCheckLength, Global.Utshig, Global.Utshig.Length))
                        {
                            return Encoding.Unicode;
                        }
                    }
                }

                /*
                // Analyze the BOM NO needed 
                if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
                if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
                if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
                if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
                if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
                if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE
                */
            }
            // We actually have no idea what the encoding is defaulting to ASCII
            return Encoding.ASCII;
        }
        private void Form1_Load(object sender, EventArgs e)
        {

            Global.Ftype = Encoding.UTF8;
            comboBox1.SelectedIndex = 0;
            listBox1.TabStop = false;
            listBox1.Enabled = false;
            button5.Enabled = false;
            label7.Visible = false;
            label8.Visible = false;
            radioButton1.Checked = true;
            //textBox4.AcceptsTab = true;
            //textBox1.AcceptsTab = true;
            //textBox2.AcceptsTab = true;
            //textBox3.AcceptsTab = true;
            
            textBox5.Text = Global.tsheMax.ToString();
            this.ActiveControl = textBox1;
            textBox1.Focus();

            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            System.IO.Stream myStream;
            listBox1.Enabled = true;
            OpenFileDialog thisDialog = new OpenFileDialog();

            int old_count = listBox1.Items.Count;
            thisDialog.InitialDirectory = Application.StartupPath;  //"c:\\";
            //thisDialog.InitialDirectory = @"F:\CurrentWorking\HuongtichEngagework\syllogism\10 - DHARAMAS";
            thisDialog.Filter = "All files (*.*)|*.*|Text Files (*.txt)|*.txt";
            thisDialog.FilterIndex = 2;
            thisDialog.RestoreDirectory = true;
            thisDialog.Multiselect = true;
            thisDialog.Title = "Select file(s) for applying search rules";
            
            if (thisDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in thisDialog.FileNames)
                {
                    try
                    {
                        if ((myStream = thisDialog.OpenFile()) != null)
                        {
                            using (myStream)
                            {
                                //MessageBox.Show(GetEncoding(file).ToString());
                                if ((GetEncoding(file) == Global.Ftype) && 
                                    (!listBox1.Items.Contains(file)) && 
                                    (file.Length <= Global.MaxFSize))
                                {
                                    FileInfo fi = new FileInfo(file);
                                    Global.lFileSizes.Add(fi.Length);
                                    listBox1.Items.Add(file);
                                    label7.Visible = true;
                                    label8.Visible = true;
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not read file. Error: " + ex.Message);
                    }
                }
            }
            if (old_count == listBox1.Items.Count)
            {
                MessageBox.Show("No new " + Global.Ftype.ToString().Substring(Global.CodingPrfx) + " Tibetan files found in the selected directory!");
            }
            else
            {
                listBox1.Sorted = true;
            }
            if (listBox1.Items.Count > 0)
                button5.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count > 0)
            {
                DialogResult result = MessageBox.Show("Clear all list files that you selected?", "Confirmation", MessageBoxButtons.OKCancel);
                if (result == DialogResult.OK)
                {
                    listBox1.Items.Clear();
                    listBox1.Enabled = false;
                }
              
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            while (listBox1.SelectedItems.Count > 0)
            {
                listBox1.Items.Remove(listBox1.SelectedItems[0]);
            }
            if (listBox1.Items.Count == 0)
                button5.Enabled = false;
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            int number;
            bool success = int.TryParse(textBox5.Text, out number);
            int index = comboBox1.SelectedIndex;
            if ((success) && (number >= Global.SMin))
            {     
                if (number > Global.tokenMax[index])
                {
                    textBox5.Text = Global.tokenMax[index].ToString();
                    MessageBox.Show("The maximum value for token " +Global.strTokenMax[index] + "is: " + 
                        Global.tokenMax[index].ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Founding block text length accept only positive number\n" +
                    "which less than 500 allowed!", "Error, invalid value entered!");
                textBox5.Text = Global.tokenMax[index].ToString(); 
            }
        }

        public byte [] StringToEncode(Encoding type, string content)
        {
            Encoding unicode = Encoding.Unicode;
            byte[] encodebytes = type.GetBytes(content);
            return encodebytes;
        }

        void process_data(string fText, string ffname)
        {
            prog.LastSaveIndx = -1;
            (string Found, bool good, int tmpIndx) found = ("", false, 0);
            do
            {
                found = Search(fText, found.tmpIndx);
                string test = ffname;
                // to reduce duplicate  the distance of two founding also must grater than Global.Slength
                if ((found.good == true) && (found.Found.Length > 0))
                {                  
                    prog.IndxSFound.Add(found.tmpIndx);
                    if ((prog.SFound.Count == 0) ||
                        (prog.LastSaveIndx < 0) ||
                        (prog.LastSaveIndx + Global.SLength * 3 / 4 < found.tmpIndx))
                    {
                        if (prog.SFound.ContainsKey(found.Found) == false)
                        {
                            if ((checkBox1.Checked) && (Global.oldfname.Length > 0) && (found.tmpIndx < Global.SLength))
                            {
                                prog.SFound.Add(found.Found, ffname + ", \n" + Global.oldfname);
                            }
                            else
                                prog.SFound.Add(found.Found, ffname);
                            ++Global.MaximumFoundItems;
                        }
                        else
                        {
                            if (prog.IgnoreDup == false)
                            {
                                var result = MessageBox.Show("Same text :" + found.Found +
                                    "\nwas found in  : " + prog.SFound[found.Found] +
                                    "\nalso found in : " + ffname +
                                    "\n Ignore the error?", "Duplicate Content",
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                                if (result == DialogResult.Yes)
                                    prog.IgnoreDup = true;
                            }
                        }
                        prog.LastSaveIndx = found.tmpIndx;
                    }
                }
                if (found.tmpIndx >= 0)
                {
                    
                    found.tmpIndx += Global.MinSTerm;
                }
                if (found.tmpIndx >= fText.Length)
                    found.tmpIndx = -1;
            }
            while (found.tmpIndx > 0);
        }
        void process_data_KangTeng()
        {
            /*I :KangTeng
                 * Read all files each file in the dir
                 *  Join ALL file in a dir to a big junk and do search     
            */
            string endOldTxt = "";
            Global.oldfname = "";
            string olddir = ""; 
            foreach (string ffname in listBox1.Items) // 
            {
              
                Global.CurrentDoneItems++;
                if (prog.SFound.Count > Global.MaximumFoundItems)
                {
                    MessageBox.Show("Found too many phrases. Please revise the searching conditions.", "Overload Errors",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
                }
                try
                {
                    if ((olddir.Length > 0) && (olddir != Path.GetDirectoryName(ffname))) 
                    {
                        olddir = "";
                        endOldTxt = "";
                        Global.oldfname = "";
                    }
                    string fText = File.ReadAllText(ffname, Global.Ftype);  // call search algorithm
                    fText = endOldTxt + fText;
                    if (Global.KeyReG.Count == 0)
                        process_data(fText, ffname);
                    else
                    {
                        var rx = new Regex(Global.KeyReG[0]);
                        Match match = rx.Match(fText);
                        while (match.Success)
                        {
                            if (prog.SFound.ContainsKey(match.Value) == false)
                            {
                                prog.SFound.Add(match.Value, ffname);
                            }
                            match = match.NextMatch();
                        }
                    }
                    //check if the old file are at the same directory? else not doing and assign ""????????
                    endOldTxt = fText.Substring(fText.Length - Global.SLength -1);
                    olddir =  Path.GetDirectoryName(ffname);
                    Global.oldfname = ffname;                    
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            prog.counter = (int)((Global.CurrentDoneItems * 100) / (Global.TotalMustSearchItems));
            Thread.Sleep(10);
        }

        //Not KangTeng
        void process_data_NoKangTeng()
        {
            //if (checkBox1.Checked == false)   
            foreach (string ffname in listBox1.Items)
            {
                //ReadAllText automatically detect the encoding
                //string fname = Path.GetFileName(ffname);
                // string dir == Path.GetDirectoryName(ffname);

                // NOT PROCESS OF REGEX yet!!!!!!!!!!!!!!!!!!!!
                //if the Index found distance less than will be next search but not record until the distance > Global.Slength *3/4??       

                
                if (prog.SFound.Count > Global.MaximumFoundItems)
                {
                    MessageBox.Show("Found too many phrases. Please revise the searching conditions.", "Overload Errors",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    break;
                }
                try
                {
                    string fText = File.ReadAllText(ffname, Global.Ftype);  // call search algorithm
                    if (Global.KeyReG.Count == 0)
                        process_data(fText, ffname);                 
                    else
                    {
                        var rx = new Regex(Global.KeyReG[0]);
                        Match match = rx.Match(fText);
                        while (match.Success)
                        {
                            if (prog.SFound.ContainsKey(match.Value) == false)
                            {
                                //prog.SFound.Add(found.Found, ffname);
                                prog.SFound.Add(match.Value, ffname);
                            }
                            match = match.NextMatch();
                        }
                    }
                }
                catch (Exception e)
                {
                    //duplicate content file
                    MessageBox.Show(e.Message);
                }
            }
            prog.counter = (int)((Global.CurrentDoneItems * 100) / (Global.TotalMustSearchItems));
            Thread.Sleep(10);

            /*
                     string str = "abc!";
                        Encoding unicode = Encoding.Unicode;
                        Encoding utf8 = Encoding.UTF8;
                        byte[] unicodeBytes = unicode.GetBytes(str);
                        byte[] utf8Bytes = Encoding.Convert( unicode, utf8, unicodeBytes );
                        Console.WriteLine( "UTF Bytes:" );
                        StringBuilder sb = new StringBuilder();
                        foreach( byte b in utf8Bytes ) {
                            sb.Append( b ).Append(" : ");
                        }
                        Console.WriteLine( sb.ToString() ); 
                            //       7A 61 CC 86 C7 BD CE B2 F0 90 85 94
                 */


        } //end process_dat_NoKanfTeng

        //return -1 if not found and indx if found
        public int SearchOR(string src, List<string> tgts)
        {
            int indx;
            foreach (string tgt in tgts) 
            {
                indx = src.IndexOf(tgt);
                if (indx >= 0)
                    return indx;
            }
            return -1;
        }

        //return -indx-1 if found any of tgt 
        // else return 0
        public int SearchEX(string src, List<string> tgts)
        {
            int indx;
            foreach (string tgt in tgts)
            {
                indx = src.IndexOf(tgt);
                if (indx >= 0)
                    return -(indx+1);
            }
            return 0;
        }

        public int SearchAND(string src, List<string> tgts)
        {
            foreach (string tgt in tgts)
            {
                if (src.IndexOf(tgt) < 0)
                    return -1;
            }
            return 0;
        }
        
        string take_leng(string str, int indx)
        {
            int lo = 0, hi = str.Length;
            Global.BegEnd = 0;
            if (indx > Global.SLength)
            {
                lo = indx - Global.SLength;
            }
            else
            {
                Global.BegEnd = 1;
            }
            if (indx + Global.SLength < hi)
            {
                hi = indx + Global.SLength;
            }
            else 
            {
                Global.BegEnd += 2;
            }
            
            return str.Substring(lo, hi - lo);
        }

        string TibFormat(string src)
        {
            string temp = src.Substring(0);
            int[] begin = { 20000, src.IndexOf(Global.CTibetan[0]), src.IndexOf(Global.CTibetan[1]) };
            int min = begin.Min();
            int[] end = { -1, src.LastIndexOf(Global.CTibetan[0]), src.LastIndexOf(Global.CTibetan[1]) };
            int max = end.Max();
            if ((max > 0) && (max < src.Length - 1))
                temp = temp.Substring(0, max + 1);
            if ((min < 20000) && (min >= 0))
                temp = temp.Substring(min + 1);

            //MessageBox.Show(temp.Substring(0, 10));
            //MessageBox.Show(temp.Substring(temp.Length - 10));
            return temp;
        }
        (string,bool,int) Search(string src, int indx)
        {
            int ret = -1;
            string src2 = "";

            /* split AND
            if AND.leng ==1; Search single then if found, take length apply other searched
            else if AND.len > 1 search AND[0] if found, take leng then apply AND[>0] and other searches
            else (AND.len==0)
                search OR, get Index0 found, then take leng and apply EX
                search OR at Index0 +1, get Indx1 found, then take leng and apply EX
                ....
            */

            if (Global.KeyAnds.Count >= 1)
            {
                List<string> temp = new List<string>(Global.KeyAnds); //do not modified global for the next round
                int AndkeyCnt = temp.Count;
                temp.RemoveAt(0);
                ret = src.IndexOf(Global.KeyAnds[0], indx);
                if (ret < 0)
                    return ("", false, -1);
                src2 = take_leng(src, ret);
                if (ret >= 0)
                {
                    if ((temp.Count > 0) && (SearchAND(src2, temp) < 0 ))
                            return ("", false, ret);
                    if ((Global.KeyExs.Count > 0) && (SearchEX(src2, Global.KeyExs) < 0))
                        return ("", false, ret);
                    //if ((Global.KeyOrs.Count > 0) && (SearchOR(src2, Global.KeyOrsE) < 0))
                    //    return ("", false, ret);
                    for (int i=0; i< Global.MAXOR; i++)
                    {
                        if (Global.KeyOrsE[i].Count == 0)
                            break;
                        else
                        {
                            if (SearchOR(src2, Global.KeyOrsE[i]) < 0)
                            {
                                return ("", false, ret);
                            }
                        }
                    }
                }
                //else if ()
                // get only Tsig or tsheg string in beg and end
                if (ret >= 0)
                {
                    src2 = TibFormat(src2);
                }
                return (src2, true, ret);
            }
            else if (Global.KeyOrsE[0].Count >= 1)
            {
                foreach (string tgt in Global.KeyOrsE[0])
                {
                    ret = src.IndexOf(tgt, indx);
                    if (ret >= 0)
                    {
                        src2 = take_leng(src, ret);
                        // now do the rest OR and Ex
                        if ((Global.KeyExs.Count > 0) && (SearchEX(src2, Global.KeyExs) < 0))
                            return ("", false, ret);
                        for (int i=1; i< Global.MAXOR; i++)
                        {
                            if (Global.KeyOrsE[i].Count == 0)
                            {
                                src2 = TibFormat(src2);
                                return (src2, true, ret);
                            }
                            else
                            {
                                if (SearchOR(src2, Global.KeyOrsE[i]) < 0)
                                {
                                    return ("", false, ret);
                                }
                            }
                        }
                    }
                }
            }
            return ("", false,-1);
        }

        int getPhraseLength()
        {
            int factor = Int32.Parse(textBox5.Text.ToString());

            switch (comboBox1.SelectedIndex)
            {
                case 0:  //་  (ཙེག་)
                    return factor * Global.LongWord;
                case 1:   //། (ཚིག་གྲུབ་)
                    return (factor * Global.MaxSearchingItems) / 8;
                default:   // Letters
                    return factor;
            }
        }

        int getMinSTerm()
        {
            int min = 20000;
            foreach (string item in Global.KeyAnds) 
            {
                min = ((min > item.Length) && (item.Length > 1))? (item.Length) : (min);
            }
            for (int i = 0; i < Global.MAXOR; i++)
            {
                if (Global.KeyOrsE[i].Count > 0)
                {
                    foreach (string item in Global.KeyOrsE[i])
                    {
                        min = ((min > item.Length) && (item.Length > 1)) ? (item.Length) : (min);
                    }
                }
                else
                    break;
            }
            return min;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            /* Good  its auto 
            string fText = File.ReadAllText(listBox1.Items[0].ToString(), Global.Ftype);
            //int res;
            //string s4 = textBox1.Text.ToString();
            //int tcnt = fText.Split('་').Length - 1;
            //་ཤིང་། །སེམས་ཅན་ལས་
            
            //string fText = "་ཤིང་། །སེམས་ཅན་ལས་་ཤིང་།།སེམས་ཅན་ལས་་ཤིང་།   །སེམས་ཅན་ལས་་ཤིང་།\n །སེམས་ཅན་ལས་";
            string res = Regex.Replace(fText, @"།\s*།", "།");
            //prog.test = "res:" + res + Environment.NewLine +"==========" + Environment.NewLine + fText;
            //Form4 frmx = new Form4();
            //frmx.Show();
            //MessageBox.Show( fText);

            int scnt = res.Split('།').Length - 1;
            //float len = (float)fText.Length / (float)(tcnt + scnt); // everage about 3.8
            float len = (float)fText.Length / (float)scnt;
            MessageBox.Show("length of a word:" + len.ToString());
            //res = fText.IndexOf(s4);  //༄༅༅། །རྒྱ་གར་སྐད་དུ།

            //MessageBox.Show(res.ToString());
            //prog.test = fText.Substring(res-1, 20) + "\t" + fText.Substring(res - 3, 20) ;
            //MessageBox.Show(fText.Substring(110,20));
            //Form4 frmx = new Form4();
            //frmx.Show();
            //textBox5.Text = fText.Substring(110, 20);
            return;
            */

            
            if ((textBox4.TextLength > 1) || (textBox1.TextLength > 1) || (textBox2.TextLength > 1))
            {
                //reset logic search value strings
                Global.KeyAnds.Clear();
                for (int i = 0; i < Global.MAXOR; i++)
                {
                    if (Global.KeyOrsE[i] != null)
                    {
                        Global.KeyOrsE[i].Clear();
                    }
                    else
                    {
                        Global.KeyOrsE[i] = new List<string>();
                    }
                }
                Global.KeyExs.Clear();
                Global.KeyReG.Clear();
                prog.SFound.Clear();
                
                if (listBox1.Items.Count < Global.MaxSearchingItems)
                {
                    //setup data total number of files must be search and current handling...
                    Global.TotalMustSearchItems = listBox1.Items.Count;
                    Global.CurrentDoneItems = 0;

                    // counting number of tokens for length limitation
                    // comboBox1.SelectedIndex: 0 =་  (ཙེག་) , 1= ། (ཚིག་གྲུབ་), 2 = Letters
                    //set leng of Search string

                    Global.SLength = Int32.Parse(textBox5.Text.ToString()); // for letters
                    Global.SToken = comboBox1.SelectedItem.ToString().Substring(0, 1);
                    switch (comboBox1.SelectedIndex)
                    {
                        case 0:
                            Global.SLength *= 4;  // 4 each word
                            break;
                        case 1:
                            Global.SLength *= 64; // 64 each phrases
                            break;
                        default: // number of char keep same                            
                            break;
                    }

                    
                    //setup key data
                    // Regex
                    if (textBox4.TextLength > 1)
                    {
                        //only 2 Regex allowed
                        Global.KeyReG = textBox4.Text.Trim().ToString().Split(Global.SplitToken, StringSplitOptions.RemoveEmptyEntries).ToList();                    
                    }
                    else 
                    {   
                        // AND and may be OR / EX 
                        if (textBox1.Text.Trim().Length > 1)
                        {
                            //textBox1.Text.ToString().Split().CopyTo(Global.KeyAnd, 0);
                            Global.KeyAnds = textBox1.Text.Trim().ToString().Split(Global.SplitToken,StringSplitOptions.RemoveEmptyEntries).ToList();
                        }
                                
                        // No AND, only OR
                        if (textBox2.Text.Trim().Length > 1)
                        {
                            string [] arrayOR = textBox2.Text.Trim().ToString().Split(Global.SplitTokenE, StringSplitOptions.RemoveEmptyEntries);
                            for (int i=0; i< arrayOR.Length; i++)
                            {
                                //distriute to ORSE
                                if (i < Global.MAXOR)
                                    Global.KeyOrsE[i] = arrayOR[i].Split(Global.SplitToken, StringSplitOptions.RemoveEmptyEntries).ToList();
                                else
                                    break;
                            }


                        }
                            //textBox2.Text.ToString().Split().CopyTo(Global.KeyOrs, 0);
                            //Global.KeyOrs = textBox2.Text.Trim().ToString().Split(Global.SplitToken, StringSplitOptions.RemoveEmptyEntries).ToList();
                        if (textBox3.Text.Trim().Length > 1)
                            //textBox3.Text.ToString().Split().CopyTo(Global.KeyExs, 0);
                            Global.KeyExs = textBox3.Text.Trim().ToString().Split(Global.SplitToken, StringSplitOptions.RemoveEmptyEntries).ToList();

                        Global.MinSTerm = getMinSTerm();
                        if (Global.MinSTerm > 2048)
                        {
                            MessageBox.Show("One of the searching filter was too short. Please revised!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    //KangTeng or not
                    Global.KangTeng = checkBox1.Checked;
                    
                    prog.counter = 5;
                    // Cal two different process data for Kangyur / Tengyur  an the NO 
                    if (checkBox1.Checked == false)
                    {
                        using (Form2 frm = new Form2(process_data_NoKangTeng))
                        {
                            frm.ShowDialog(this);
                        }
                    }
                    else
                    {
                        using (Form2 frm = new Form2(process_data_KangTeng))
                        {
                            frm.ShowDialog(this);
                        }
                    }

                    // results
                    if (prog.SFound.Count > 0)
                    {
                        using (Form3 frmFound = new Form3())
                        {
                            frmFound.ShowDialog(this);
                        }
                    }
                    else
                    {
                        MessageBox.Show("No text matched!\nPlease fine tunning the filters to get better result");
                    }
                }
                else
                {
                    MessageBox.Show("The amount of selected files for searching are too large ( > " + Global.MaxSearchingItems.ToString() +")." +
                        "\nPlease reduce the list of files and try again.", "Overloaded number of files!", MessageBoxButtons.OK, MessageBoxIcon.Error); ;
                }
            }
            else
                MessageBox.Show("Please fill in the finding searching texts\nat least in either box 1, or 4, then click Start Searching button again.", "The target searching strings not edited!");

        }
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if ((listBox1.Items.Count > 0) && (Global.Ftype == Encoding.Unicode))
            {
                radioButton2.Checked = true;
            }
            else
            {
                Global.Ftype = Encoding.UTF8;
            }
        }
        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {         
            if ((listBox1.Items.Count > 0) && (Global.Ftype == Encoding.UTF8))
            {
                radioButton1.Checked = true;
            }
        }

        private void radioButton1_Click(object sender, EventArgs e) 
        {
            if ((listBox1.Items.Count > 0) && (Global.Ftype == Encoding.Unicode))
            {
                MessageBox.Show("If you change to UTF8 format, then you have to select\n" +
                    "new files for check Unicode and searching.\n" +
                    "To do so please click Delete All", "Wrong Button Click", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Global.Ftype = Encoding.UTF8;
            }
        }

        private void radioButton2_Click(object sender,  EventArgs e)
        {
            if ((listBox1.Items.Count > 0) && (Global.Ftype == Encoding.UTF8))
            {
                MessageBox.Show("If you change to Unicode format, then you have to select\n" +
                    "new files for check Unicode and searching.\n" +
                    "To do so please click Delete All", "Wrong Button Click",MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Global.Ftype = Encoding.Unicode;
            }
        }
        
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
            switch (comboBox1.SelectedIndex)
            {
                case 0:  //་  (ཙེག་)
                    textBox5.Text =Global.tsheMax.ToString();
                    break;
                case 1:   //། (ཚིག་གྲུབ་)
                    textBox5.Text = Global.tsigMax.ToString();
                    break;
                default:   // Letters
                    textBox5.Text = Global.phraseMax.ToString();
                    break;
            }
        }
        static bool isSubArray(byte[] A, int LenA, byte[] Sub, int LenSub)
        {
            int i = 0, j = 0;

            // Traverse both arrays simultaneously 
            while (i < LenA && j < LenSub)
            {
                if (A[i] == Sub[j])
                {
                    i++;
                    j++;
                    if (j == LenSub)
                        return true;
                }

                // increase i and reset j 
                else
                {
                    i = i - j + 1;
                    j = 0;
                }
            }
            return false;
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (textBox4.TextLength > 0)
            {
                groupBox1.Enabled = false;
            }
            else
            {
                groupBox1.Enabled = true;
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void textBox1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.Shift)
            //    if (e.KeyData == Keys.Tab)
            {
                textBox1.Text += Global.SplitToken[0];
                textBox1.Select(textBox1.Text.Length, 0);
                e.IsInputKey = true;
            }
        }

        private void textBox2_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            //if (e.KeyData == Keys.Tab)
            if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.Shift)
            {
                textBox2.Text += Global.SplitToken[0];
                textBox2.Select(textBox2.Text.Length, 0);
                e.IsInputKey = true;
            }
        }

        private void textBox3_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            //if (e.KeyData == Keys.Tab)
            if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.Shift)
            {
                textBox3.Text += Global.SplitToken[0];
                textBox3.Select(textBox3.Text.Length, 0);
                e.IsInputKey = true;
            }
        }

        private void textBox4_PreviewKeyDown(object sender , PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Tab && e.Modifiers == Keys.Shift)
            //if (e.KeyData == Keys.Tab)
            {
                textBox4.Text += '\t';
                textBox4.Select(textBox4.Text.Length, 0);
                e.IsInputKey = true;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
    }

    static class prog
    {
        public static int counter;
        public static string test = "";
        //public static List<string> SFound = new List<string>();
        public static Dictionary<string, string> SFound = new Dictionary<string, string>();
        public static List<int> IndxSFound = new List<int> ();
        public static int LastSaveIndx = -1;
        public static bool IgnoreDup = false;
        //to notify the founding text related to earlier file
        //public static bool gotKangTeng = false;
    } 
}
