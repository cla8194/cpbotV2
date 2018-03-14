using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;


namespace CPBot_v2 {

	class Brain {
		// https://discordapp.com/api/oauth2/authorize?client_id=282945253479022593&scope=bot&permissions=0
		string discordToken = "MjgyOTQ1MjUzNDc5MDIyNTkz.C4y3XA.aolLg5Vrk_L_gIS3QJsxRwT-ALo"; //  282945253479022593    sKfa_v_UelvrAzwgUFlsl3NpxkWVxtWL
		public string fileConversation = "conversation.txt";
		public string botName = "Cpbot";
		public Dictionary<string, List<string>> lines = new Dictionary<string, List<string>>();
		public List<string> linesLinear = new List<string>();

		static void Main(string[] args)
			=> new Brain().StartAsync().GetAwaiter().GetResult();

		private DiscordSocketClient client;
		private BotCommands botCommands;

		public async Task StartAsync() {
			client = new DiscordSocketClient();
			await client.LoginAsync(TokenType.Bot, discordToken);
			await client.StartAsync();
			botCommands = new BotCommands(client, this);
			if (!File.Exists(fileConversation)) { 
				FileStream f = File.Create(fileConversation); 
				f.Close(); 
			}
			else {
				string[] a = File.ReadAllLines(fileConversation);
				for (int i=0; i<a.Length - 1; i++) {
					string key = a[i];
					AddConversationElement(key, a[i+1]);
				}
				linesLinear.Add(a[a.Length - 1]);
			}
			await client.SetGameAsync("--help");
			await Task.Delay(-1);
		}


		public void AddConversationElement(string key, string answer) {
			key = key.ToLower();
			if (!linesLinear.Contains(answer)) linesLinear.Add(answer);

			if (lines.ContainsKey(key)) {
				lines[key].Add(answer);
			}
			else {
				lines.Add(key, new List<string>() { answer });
			}
		}

		public string ReplaceFirst(string str, string old, string to) {
			int i = str.IndexOf(old);
			if (i > -1) return str.Substring(0, i) + to + str.Substring(i + old.Length);
			return str;
		}
		public bool Contains(string str, params string[] s) {
			foreach (string t in s) {
				if (str.Contains(t))
					return true;
			}
			return false;
		}


	}




}
