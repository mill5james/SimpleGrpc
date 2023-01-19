using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grpc.Net.Client;
using HelloWorld;

namespace GrpcWinForms
{
    public partial class Form1 : Form
    {
        private readonly Service.ServiceClient client;
        private readonly GrpcChannel channel;
        public Form1()
        {
            InitializeComponent();
            //Read the docs at https://docs.microsoft.com/en-us/aspnet/core/grpc/netstandard?view=aspnetcore-5.0
            channel = GrpcChannel.ForAddress("https://localhost:5001", new GrpcChannelOptions
            {
                HttpHandler = new WinHttpHandler()
            });

            client = new Service.ServiceClient(channel);
        }


        private async void sendButton_ClickAsync(object sender, EventArgs e)
        {
            var message = inputBox.Text;
            var reply = await client.HelloAsync(
                new Request{ Name = message });
            messageList.Items.Add(reply.Message);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            channel.Dispose();
        }
    }
}
