﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DCPU_16.Forms;
using DCPU_16.Properties;

namespace DCPU_16
{
    public partial class Form1 : Form
    {
        public readonly int ToolBoxWidth = 200;
        public Executor E;
        public ExecuteForm D = new ExecuteForm();
        private const string Filter = "Program files | *.dasm16";
        private const string Capiton = "DCPU-16";
        private readonly Code _code;
        private const int CodeSize = 99;
        private readonly HelpForm _help = new HelpForm();
        public static bool HasDump { get; set; }

        public Form1()
        {
            InitializeComponent();

            WorkSpace.Panel2.Visible = false;
            WorkSpace.SplitterDistance = Width;

            _code = new Code(CodeSize);

            foreach (var command in Decryptor.Commands)
            {
                CommandBox.Items.Add(command);
            }
            CommandBox.SelectedIndex = 0;

            D.Show();
            HasDump = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var m = MessageBox.Show(Resources.ExitString, Capiton, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (m == DialogResult.Yes)
            {
                Close();
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.Items.Count > 0)
            {
                var m = MessageBox.Show(Resources.SaveString, Capiton, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (m == DialogResult.Yes)
                {
                    Save();
                }
            }

            Program.Items.Clear();
            CommandBox.SelectedIndex = 0;
            WorkSpace.Panel2.Visible = true;
            WorkSpace.SplitterDistance = Width - ToolBoxWidth;
        }

        private void Save()
        {
            var s = new SaveFileDialog { Filter = Filter };
            if (s.ShowDialog() == DialogResult.OK)
            {
                IO.Write(s.FileName, _code.ProgramArray);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (!WorkSpace.Panel2.Visible)
            {
                WorkSpace.SplitterDistance = Width;
                return;
            }

            if (Width > ToolBoxWidth)
            {
                WorkSpace.SplitterDistance = Width - ToolBoxWidth;
            }
        }

        private void Hide_Click(object sender, EventArgs e)
        {
            WorkSpace.Panel2.Visible = false;
            WorkSpace.SplitterDistance = Width;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var o = new OpenFileDialog { Filter = Filter };
            if (o.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            _code.ProgramArray = IO.Read(o.FileName, 99);
            for (var i = 0; i < _code.MaxLength; i++)
            {
                WriteInstruction(_code.ProgramArray[i], i);
            }
        }

        private void WriteInstruction(int value, int index)
        {
            if (value > Processor.MaxValue || value < Processor.MinValue)
            {
                return;
            }

            if (Program.Items.Count > index)
            {
                Program.Items.RemoveAt(index);
            }

            string instruction;
            Decryptor.Commands.TryGetValue(value / 100, out instruction);
            Program.Items.Insert(index, String.Format("{0:00}:\t{1}\t{2}\t[{3}]", index, instruction, value % 100, Convert.ToString(value, 16)));
        }

        private void Run_Click(object sender, EventArgs e)
        {
            E = new Executor { P = new Processor(100) };

            if (!E.P.LoadMemory(_code.ProgramArray))
            {
                return;
            }

            D.UpdateDamp(E.GetDump(0, 30));
            E.Execute();
            D.UpdateOutput(Executor.Output.ToString());
            D.UpdateDamp(E.GetDump(0, 30));
        }

        private void toolBoxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WorkSpace.Panel2.Visible = true;
            WorkSpace.SplitterDistance = Width - ToolBoxWidth;
        }

        private void outputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (HasDump)
            {
                D.Close();
                return;
            }

            D = new ExecuteForm();
            D.Show();
            if (E != null)
            {
                D.UpdateDamp(E.GetDump(0, 30));
            }
        }

        private void AddCommand_Click(object sender, EventArgs e)
        {
            try
            {
                var index = Int32.Parse(CommandAddress.Text);
                
                int command;
                Decryptor.Instructions.TryGetValue(CommandBox.SelectedIndex, out command);

                //to fix annoying bug
                command++;

                ushort a = 0x0000;

                a = (ushort)(0x0000 | command);

                Console.WriteLine("a " + Convert.ToString(a, 2));

                var operand = Int32.Parse(OperandAddress.Text);

                //our registers
                int operand2;
                
                Console.WriteLine("reg " + OperandAddress2.Text);
                D.UpdateOutput(Executor.Output.ToString());         //throws exception
                Decryptor.Registers.TryGetValue(OperandAddress2.Text.ToString().ToUpper(), out operand2);
                Console.WriteLine("operand2 " + operand2);

                ushort b = (ushort)(0x0084 + (operand << 2));
                Console.WriteLine("b antes shift" + Convert.ToString(b, 2));

                a = (ushort)(operand2 | a);
                b = (ushort)(b << 8);
                Console.WriteLine("b " + Convert.ToString(b, 2));
                a = (ushort)(b | a);
                Console.WriteLine("a " + Convert.ToString(a, 2));
                //ushort value = (ushort)(operand << 10);
                //a |= value;

                //var result = command*100 + operand;
                Console.WriteLine("a " + a);

                ushort result = a;
                if (_code.Add(index, result))
                {
                    Console.WriteLine("here");
                    WriteInstruction(result, index);
                }   //throws exception
            }
            catch (Exception)
            {
                return;
            }
        }

        private void AddData_Click(object sender, EventArgs e)
        {
            try
            {
                var index = Int32.Parse(DataAdress.Text);
                var result = Int32.Parse(DataValue.Text);

                if (_code.Add(index, (ushort)result))
                {
                    WriteInstruction(result, index);
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void ClearCell_Click(object sender, EventArgs e)
        {
            try
            {
                var index = Int32.Parse(CellAddress.Text);
                if (_code.Add(index, 0))
                {
                    WriteInstruction(0, index);
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            Run.Enabled = true;
            File.Enabled = true;
            WorkSpace.Panel2.Enabled = true;
            Stop.Enabled = false;
            doStepToolStripMenuItem.Enabled = false;
            startDebuggingToolStripMenuItem.Enabled = false;
        }

        private void startDebuggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Run.Enabled = false;
            File.Enabled = false;
            WorkSpace.Panel2.Enabled = false;
            doStepToolStripMenuItem.Enabled = true;
            startDebuggingToolStripMenuItem.Enabled = true;
            Stop.Enabled = true;

            E = new Executor { P = new Processor(100) };
            if (!E.P.LoadMemory(_code.ProgramArray))
            {
                return;
            }

            D.UpdateDamp(E.GetDump(0, 30));
            E.Prepearing();
            D.UpdateOutput(Executor.Output.ToString());
        }

        private void doStepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (E.DoStep())
            {
                D.UpdateDamp(E.GetDump(0, 30));
                D.UpdateOutput(Executor.Output.ToString());
                Program.SelectedIndex = E.P.PC;
            }
            else
            {
                // E.CheckErrors();
                D.UpdateOutput(Executor.Output.ToString());

                Run.Enabled = true;
                File.Enabled = true;
                WorkSpace.Panel2.Enabled = true;
                Stop.Enabled = false;
                doStepToolStripMenuItem.Enabled = false;
                startDebuggingToolStripMenuItem.Enabled = false;
            }
        }

        private void commandsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _help.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Resources.AboutString, Capiton);
        }

        private void Program_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataAdress.Text = Program.SelectedIndex.ToString();
            CommandAddress.Text = Program.SelectedIndex.ToString();
            CellAddress.Text = Program.SelectedIndex.ToString();
        }
    }
}
