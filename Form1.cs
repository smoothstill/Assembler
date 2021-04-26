using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Assembler
{
    public partial class Form1 : Form
    {

        private string input;
        private Dictionary<string, string> symbolsDictionary;
        private Dictionary<string, string> parseCDest;
        private Dictionary<string, string> parseCComp;
        private Dictionary<string, string> parseCJump;
        private Dictionary<int, int> lineNumbers;
        private StringBuilder binaryOutput;

        public Form1()
        {
            binaryOutput = new StringBuilder();
            lineNumbers = new Dictionary<int, int>();
            InitializeCInstructionDictionary();
            InitializeComponent();

        }

        /// <summary>
        /// Click the button to browse for an assembly file.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            lineNumbers.Clear();
            int size = -1;
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                try
                {
                    input = null;
                    button2.Enabled = false;
                    Text = "Assembler";
                    input = File.ReadAllText(file);
                    size = input.Length;
                    //button2.Enabled = true;

                    binaryOutput.Clear();

                    WriteHackFile(input);


                }
                catch (IOException ex)
                {
                    MessageBox.Show(ex.Message);
                    button2.Enabled = false;
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ERROR: " + ex.Message);
                    return;
                }

                button2.Enabled = true;
                Text = "Assembler - File loaded succesfully";

            }
            //Console.WriteLine(size); // <-- Shows file size in debugging mode.
            //Console.WriteLine(result); // <-- For debugging use.
        }

        /// <summary>
        /// Click the button to save the hack file.
        /// </summary>
        private void button2_Click(object sender, EventArgs e)
        {
            // When user clicks button, show the dialog.
            saveFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {


            //delete last line end \n
            binaryOutput.Remove(binaryOutput.Length - 1, 1);

            // Get file name.
            string name = saveFileDialog1.FileName;
            // Write to the file name selected.
            // ... You can write the text from a TextBox instead of a string literal.
            File.WriteAllText(name, binaryOutput.ToString());

            Text = "Assembler - File saved succesfully";

        }

        /// <summary>
        /// Removes all the useless stuff from the assembly code (spaces, tabs, commments)
        /// Removes Jump declarations and saves their ROM position to dictionary.
        /// Passes the result string to a SymbolTable function.
        /// </summary>
        /// <param name="text">Assembly code as plaintext</param>
        private void WriteHackFile(string text)
        {
            InitializeRAMDictionary();

            StringBuilder sb = new StringBuilder();
            StringBuilder output = new StringBuilder();
            StringReader strReader = new StringReader(text);

            bool lastLineWasLabelDeclaration = false;
            string line = null;
            int lineNumber = 0;
            int PC = 0;
            int i = 0;

            while (true)
            {
                line = strReader.ReadLine();

                if (line != null)
                {
                    ++lineNumber;
                    int end = line.IndexOf('/');

                    if (end < 0)
                        end = line.Length;
                    
                    sb.Append(line, 0, end);
                    sb.Replace(" ", "");
                    sb.Replace("\t", "");

                    if (sb.Length > 0)
                    {
                        if (sb[0] == '(' && sb[sb.Length - 1] == ')')
                        {
                            line = sb.ToString().Substring(1, sb.Length - 2);
                            if (symbolsDictionary.ContainsKey(line)) throw new Exception("Line " + lineNumber + ":There can be only one label declaration for every label");
                            symbolsDictionary.Add(line, (PC).ToString());
                            sb.Clear();
                            lastLineWasLabelDeclaration = true;
                            continue;
                        }

                        lastLineWasLabelDeclaration = false;
                        output.Append(sb + "\n");

                        ++i;
                        lineNumbers.Add(i, lineNumber);

                        ++PC;
                        sb.Clear();
                    }
                }
                else
                {
                    if (lastLineWasLabelDeclaration == true)
                    {
                        throw new Exception("Last line cannot be a label declaration");
                    }

                    break;
                }

 
            }

            if (output.Length == 0) throw new Exception("file was empty");

            //delete last line end \n
            output.Remove(output.Length - 1, 1);

            //Console.Write(output.ToString());

            DecodeSymbols(output.ToString());

            

        }

        /// <summary>
        /// Removes A-instruction symbols and turns them into appropriate number values (in string form).
        /// Passes A-instructions to AInstructionToBinary function (without the @ symbol).
        /// Passes C-instructions to CInstructionParse function.
        /// </summary>
        /// <param name="input"></param>
        private void DecodeSymbols(string input)
        {

            StringBuilder sb = new StringBuilder();
            StringBuilder output = new StringBuilder();
            StringReader strReader = new StringReader(input);

            string line = null;
            int number = 0;
            int lineNumber = 0;

            int nextRAM = 16; 

            while (true)
            {
                line = strReader.ReadLine();

                if (line != null)
                {
                    ++lineNumber;
                    if (line[0] == '@')
                    {
                        line = line.Substring(1, line.Length - 1);

                        if (Int32.TryParse(line, out number))
                        {
                            if (number < 0 || number > 32767) throw new Exception("Line " + lineNumbers[lineNumber] + ":MSB of an A-instruction has to be 0 (value has to be positive)");
                            else
                            {
                                AInstructionToBinary(line);
                                continue;
                            }
                        }

                        if (line[0] == '0' || line[0] == '1' || line[0] == '2' || line[0] == '3' || line[0] == '4' || line[0] == '5' || line[0] == '6' || line[0] == '7' || line[0] == '8' || line[0] == '9')
                            throw new Exception("Line " + lineNumbers[lineNumber] + ":A-instruction " + line + " cannot start with a number");

                        if (symbolsDictionary.ContainsKey(line))
                        {
                            AInstructionToBinary(symbolsDictionary[line]);
                        }
                        else
                        {
                            symbolsDictionary.Add(line, nextRAM.ToString());
                            AInstructionToBinary(symbolsDictionary[line]);
                            ++nextRAM;
                        }

                    }
                    else
                    {
                        CInstructionParse(line, lineNumber);
                    }
                    
                }
                else
                {
                    break;
                }


            }


        }

        /// <summary>
        /// Splits C instruction string into 3 parts: dest, comp and jump.
        /// Passes these parts to a function CInstructionToBinary.
        /// </summary>
        /// <param name="command">C-Instruction</param>
        /// <param name="lineNumber">Line number of the assembly plaintext file we are currently working on.</param>
        private void CInstructionParse(string command, int lineNumber)
        {
            //split to parts:
            //dest=comp;jump
            //(both dest and jump are optional)

            string dest;
            string comp;
            string jump;

            int splitA = command.IndexOf('=');

            if (splitA >= 0)
            {
                dest = command.Substring(0, splitA);
            }
            else
            {
                dest = null;
            }

            int splitB = command.IndexOf(';');
            if (splitB >= 0)
            {
                comp = command.Substring(splitA + 1, splitB - splitA - 1);
                jump = command.Substring(splitB + 1, command.Length - splitB - 1);
            }
            else
            {
                jump = null;
                comp = command.Substring(splitA + 1, command.Length - splitA - 1);
            }

            CInstructionToBinary(dest, comp, jump, lineNumber);
            
        }

        /// <summary>
        /// Writes the C-instruction into binary form.
        /// Uses dictionary to change dest, comp and jump parts into their binary values.
        /// </summary>
        /// <param name="dest">dest part of the C-instruction</param>
        /// <param name="comp">comp part of the C-instruction</param>
        /// <param name="jump">jump part of the C-instruction</param>
        /// <param name="lineNumber">Line number of the assembly plaintext file we are currently working on.</param>
        private void CInstructionToBinary(string dest, string comp, string jump, int lineNumber)
        {

            StringBuilder binary = new StringBuilder("111");

            if (comp != null)
            {

                if (parseCComp.ContainsKey(comp))
                    binary.Append(parseCComp[comp]);
                else
                    throw new Exception("Line " + lineNumbers[lineNumber] + ": invalid comp value");

            }
            else
                throw new Exception("Line " + lineNumbers[lineNumber] + ": comp value cannot be null");


            if (dest != null)
            {

                if (parseCDest.ContainsKey(dest))
                    binary.Append(parseCDest[dest]);
                else
                    throw new Exception("Line " + lineNumbers[lineNumber] + ": invalid dest value");

            }
            else
            {
                binary.Append("000");
            }

            if (jump != null)
            {

                if (parseCJump.ContainsKey(jump))
                    binary.Append(parseCJump[jump]);
                else
                    throw new Exception("Line " + lineNumbers[lineNumber] + ": invalid jump value");

            }
            else
            {
                binary.Append("000");
            }

            binaryOutput.Append(binary + "\n");

        }

        /// <summary>
        /// Turns A-instruction into its binary form
        /// Changes A-instruction into a signed 16-bit integer, then converts it to string
        /// as a 16-bit fixed length binary form. Doesn't need to check if number value is 
        /// positive because it has been checked already.
        /// </summary>
        /// <param name="command">A-instruction</param>
        private void AInstructionToBinary(string command)
        {
            Int16 binary = 0;
            binary = Int16.Parse(command);
            command = Convert.ToString(binary, 2).PadLeft(16, '0');
            binaryOutput.Append(command.ToString() + "\n");
        }


        private void InitializeRAMDictionary()
        {
            symbolsDictionary = new Dictionary<string, string>();
            symbolsDictionary.Add("R0", "0");
            symbolsDictionary.Add("R1", "1");
            symbolsDictionary.Add("R2", "2");
            symbolsDictionary.Add("R3", "3");
            symbolsDictionary.Add("R4", "4");
            symbolsDictionary.Add("R5", "5");
            symbolsDictionary.Add("R6", "6");
            symbolsDictionary.Add("R7", "7");
            symbolsDictionary.Add("R8", "8");
            symbolsDictionary.Add("R9", "9");
            symbolsDictionary.Add("R10", "10");
            symbolsDictionary.Add("R11", "11");
            symbolsDictionary.Add("R12", "12");
            symbolsDictionary.Add("R13", "13");
            symbolsDictionary.Add("R14", "14");
            symbolsDictionary.Add("R15", "15");
            symbolsDictionary.Add("SCREEN", "16384");
            symbolsDictionary.Add("KBD", "24576");
            symbolsDictionary.Add("SP", "0");
            symbolsDictionary.Add("LCL", "1");
            symbolsDictionary.Add("ARG", "2");
            symbolsDictionary.Add("THIS", "3");
            symbolsDictionary.Add("THAT", "4");

        }


        private void InitializeCInstructionDictionary()
        {
            parseCDest = new Dictionary<string, string>();
            parseCDest.Add("M", "001");
            parseCDest.Add("D", "010");
            parseCDest.Add("MD", "011");
            parseCDest.Add("A", "100");
            parseCDest.Add("AM", "101");
            parseCDest.Add("AD", "110");
            parseCDest.Add("AMD", "111");

            parseCComp = new Dictionary<string, string>();
            parseCComp.Add("0", "0101010");
            parseCComp.Add("1", "0111111");
            parseCComp.Add("-1", "0111010");
            parseCComp.Add("D", "0001100");
            parseCComp.Add("A", "0110000");
            parseCComp.Add("!D", "0001101");
            parseCComp.Add("!A", "0110001");
            parseCComp.Add("-D", "0001111");
            parseCComp.Add("-A", "0110011");
            parseCComp.Add("D+1", "0011111");
            parseCComp.Add("A+1", "0110111");
            parseCComp.Add("D-1", "0001110");
            parseCComp.Add("A-1", "0110010");
            parseCComp.Add("D+A", "0000010");
            parseCComp.Add("D-A", "0010011");
            parseCComp.Add("A-D", "0000111");
            parseCComp.Add("D&A", "0000000");
            parseCComp.Add("D|A", "0010101");

            parseCComp.Add("M", "1110000");
            parseCComp.Add("!M", "1110001");
            parseCComp.Add("-M", "1110011");
            parseCComp.Add("M+1", "1110111");
            parseCComp.Add("M-1", "1110010");
            parseCComp.Add("D+M", "1000010");
            parseCComp.Add("D-M", "1010011");
            parseCComp.Add("M-D", "1000111");
            parseCComp.Add("D&M", "1000000");
            parseCComp.Add("D|M", "1010101");

            parseCJump = new Dictionary<string, string>();
            parseCJump.Add("JGT", "001");
            parseCJump.Add("JEQ", "010");
            parseCJump.Add("JGE", "011");
            parseCJump.Add("JLT", "100");
            parseCJump.Add("JNE", "101");
            parseCJump.Add("JLE", "110");
            parseCJump.Add("JMP", "111");
        }

    }
}
