using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using Discord.WebSocket;
using Discord.Commands;
using Discord;
using YoutubeSearch;


namespace CPBot_v2 {
	class BotCommands {

		private DiscordSocketClient client;
		private Brain brain;
		LevenshteinDistance match;
		private string[] botPrefix = { "cpbot ", "cpb ", "bot ", "-", "- " };
		private string lastMessage = "";
		private float simWeight = 0.75f;
		private bool useSimilarityControl = true;
		private bool useDebugMode = true;
		List<GuildEmote> emotes = new List<GuildEmote>();

		public BotCommands(DiscordSocketClient client, Brain brain) {
			this.client = client;
			this.brain = brain;
			match = new LevenshteinDistance();
			//service = new CommandService();
			//service.AddModulesAsync(Assembly.GetEntryAssembly());

			client.MessageReceived += MessageReceived;
		}


		private async Task MessageReceived(SocketMessage s) {
			var msg = s as SocketUserMessage;
			SocketGuildChannel guild = s.Channel as SocketGuildChannel;

			if (msg == null)
				return;

			if (msg.Author.Username.ToLower() == "cpbot" || !brain.Contains(s.Channel.Name.ToLower(), "tes", "generale", "bot", "musica"))
				return;

			Console.WriteLine(msg.Author.Username + " :> " + msg.ToString());
			var context = new SocketCommandContext(client, msg);

			
			

			int prefixPos = 0;
			bool prefixed = false;
			string botPrefixUsed = "";
			foreach (string pref in botPrefix) {
				if (msg.HasStringPrefix(pref, ref prefixPos)) {
					prefixed = true;
					botPrefixUsed = pref;
					break;
				}
			}

			if (prefixed) {
				string msgClean = msg.ToString().Substring(botPrefixUsed.Length).Trim(' ');
				if (Commands(msgClean.ToLower(), context))
					return;

				if (brain.lines.ContainsKey(msgClean)) {
					List<string> responses = brain.lines[msgClean];
					if (useDebugMode) await context.Channel.SendMessageAsync("`[DEBUG] message matches: " + responses.Count + "`");
					Random r = new Random();
					string resp = responses[r.Next(responses.Count)];
					resp = ResponseFormat(resp, s);
					await context.Channel.SendMessageAsync(resp);
				}
				else {
					string[] similar = Similar(msgClean, context);
					if (similar.Length > 0) {
						if (useDebugMode) await context.Channel.SendMessageAsync("`[DEBUG] sim total: " + similar.Length + "`");
						Random r = new Random();
						string randomMsg = similar[r.Next(similar.Length)].ToLower();
						List<string> responses = brain.lines[randomMsg];
						string resp = responses[r.Next(responses.Count)];
						resp = ResponseFormat(resp, s);
						await context.Channel.SendMessageAsync(resp);
					}
					else {
						if (useDebugMode) await context.Channel.SendMessageAsync("`[DEBUG] No matches or similarities has been found for: '"+ msgClean +"'`");
						string n = msg.Author.Username;
						string[] negativeResponse = { n + ", scusami ma non so ancora un cazzo e non posso risponderti.", "Non lo so, " + n, "Ma parli con me " + n + "?", n + ", non sono stato programmato per questo.", n + " dovresti chiederlo a WolfCorp.", "Non ho capito " + n, "Fammi pensare..", n + " riformula la frase, è incomprensibile.", n + ", ma stai zitto!", "Eh? <:wow2:233346621541187585> ", "Non ne ho idea!", "Non so risponderti, " + n, "Hai mai visto il fantasma formaggino, " + n + "?", "Chiedilo a Tiz, lui ha tutte le risposte <:wow2:233346621541187585> ", n + " rileggi ciò che hai scritto e dimmi se non è una cazzata.", n + ", per favore evita di renderti ridicolo dimostrando la tua ignoranza.", n + " la porta per uscire è da quella parte.", " <:ridere:238776786584993792> ", "Che? <:dayum:276779411381026816> ", n + " non posso risponderti, sono al cesso ora.", n + ", scusa ma non conosco quella parola!", n + " is WolfCorp confirmed.", n + " ma dove hai imparato a scrivere?" };
						Random r = new Random();
						await context.Channel.SendMessageAsync(negativeResponse[r.Next(negativeResponse.Length)]);
					}
				}

				if (lastMessage == "") {
					lastMessage = msgClean.ToString();
					brain.linesLinear.Add(lastMessage);
				}
				else {
					brain.AddConversationElement(lastMessage, msgClean.ToString());
					lastMessage = msgClean.ToString();
				}
			}
			else {
				if (lastMessage == "") {
					lastMessage = msg.ToString();
					brain.linesLinear.Add(lastMessage);
				}
				else {
					brain.AddConversationElement(lastMessage, msg.ToString());
					lastMessage = msg.ToString();
				}
			}

			ReactionsAuto(msg, guild);
			//await context.Channel.SendMessageAsync("lezzo " + context.Channel.Name);
		}


		private string ResponseFormat(string resp, SocketMessage s) {
			Random r = new Random();
			while (resp.Contains("##")) {
				var usersCollection = client.GetChannel(s.Channel.Id).Users;
				List<SocketUser> users = new List<SocketUser>(usersCollection);
				foreach (SocketUser user in users) { if (user.Username == brain.botName) users.Remove(user); }
				resp = brain.ReplaceFirst(resp, "##", users[r.Next(users.Count)].Username);
			}
			return resp;
		}

		private string[] Similar(string msgClean, SocketCommandContext context) {
			// ritorna keys di frasi simili a msg
			if (!useSimilarityControl) return new string[] { };

			List<string> sim = new List<string>();
			int matchTresh = (int)Math.Sqrt(msgClean.Length * simWeight);
			if (matchTresh > 5) matchTresh = 5;
			if (matchTresh < 1) matchTresh = 1;

			for (int i = 0; i < brain.linesLinear.Count - 1; i++) {
				if (match.Compute(brain.linesLinear[i], msgClean) <= matchTresh)
					sim.Add(brain.linesLinear[i]);
				else if (msgClean.Length > 7 && brain.linesLinear[i].Length > 5 && msgClean.Contains(brain.linesLinear[i]))
					sim.Add(brain.linesLinear[i]);
			}

			if (useDebugMode) {
				string d = "";
				foreach (string f in sim) {
					List<string> entries = brain.lines[f];
					d += "** " + f + " (" + entries.Count.ToString() + " entr" + (entries.Count == 1 ? "y" : "ies") + ")\n";
					if (entries.Count > 0) {
						d += "   ";
						foreach (string e in entries) {
							d += e + ", ";
						}
						d = d.TrimEnd(' ').TrimEnd(',');
						d += "\n";
					}
				}
				if (d!="") context.Channel.SendMessageAsync("`[DEBUG] Similarities found:\n" + d + "`");
			}

			return sim.ToArray();
		}

		private bool Commands(string line, SocketCommandContext context) {
			bool admin = context.Message.Author.Username.ToLower().Contains("cla");

			if (line == "-count") {
				context.Channel.SendMessageAsync("`Messages count: " + brain.linesLinear.Count.ToString() + "`");
				return true;
			}
			else if (line == "-sim" || line == "-similar") {
				context.Channel.SendMessageAsync("`Similarities for '" + lastMessage + "': " + Similar(lastMessage, context).Length.ToString() + "`");
				return true;
			}
			else if (line.Contains("-simw")) {
				string a = line.Substring(line.IndexOf("-simw") + "-simw".Length);
				if (a.Length > 0) {
					simWeight = float.Parse(a.Trim(' ')) * 0.1f;
					if (simWeight < 0.5f) simWeight = 0.5f;
					if (simWeight > 4) simWeight = 4;
					context.Channel.SendMessageAsync("`Set similarity weight to: " + (simWeight * 10f).ToString() + "`");
				}
				else {
					context.Channel.SendMessageAsync("`Parametro invalido, pirla`");
				}
				return true;
			}
			else if (line == "-last") {
				context.Channel.SendMessageAsync("`Last message: " + lastMessage + "`");
				return true;
			}
			else if (line.Contains("-remove")) {
				string a = line.Substring(line.IndexOf("-remove") + "-remove".Length).Trim(' ');
				if (a.Length > 0) {
					if (brain.lines.ContainsKey(a)) {
						List<string> vals = brain.lines[a];
						brain.lines.Remove(a);
						brain.linesLinear.Remove(a);
						foreach (string ss in vals) {
							brain.linesLinear.Remove(ss); 
						}
						context.Channel.SendMessageAsync("`Rimossa entry: '"+ a +"'`");
					}
					else {
						context.Channel.SendMessageAsync("`Key non trovata`");
					}
				}
				else {
					context.Channel.SendMessageAsync("`Parametro invalido, pirla`");
				}
				return true;
			}
			else if (line == "-debug" && admin) {
				useDebugMode = !useDebugMode;
				context.Channel.SendMessageAsync("`Debug Mode: " + (useDebugMode ? "Enabled" : "Disabled") + "`");
				return true;
			}
			else if (line == "-quit" || line == "-save") {
				context.Channel.SendMessageAsync("`Salvo le conversazioni...`");
				File.WriteAllLines(brain.fileConversation, brain.linesLinear.ToArray());
				if (line == "-quit") { if (admin) context.Channel.SendMessageAsync("`Cpbot: quit`"); }
				else context.Channel.SendMessageAsync("`Ho salvato`");
				return true;
			}
			else if (line == "-ping") {
				context.Channel.SendMessageAsync(context.User.Mention);
				context.Channel.SendMessageAsync("`Pong`");
				return true;
			}
			else if (line.Contains("-youtube")) {
				string a = line.Substring(line.IndexOf("-youtube") + "-youtube".Length);
				if (a.Length > 0) {
					var search = new VideoSearch();
					foreach (var item in search.SearchQuery(a.Trim(' '), 1)) {
						Console.WriteLine(item.Title);
						context.Channel.SendMessageAsync(item.Url);
						break;
					}
				}
				else {
					context.Channel.SendMessageAsync("`Parametro invalido, pirla`");
				}
				return true;
			}
			else if (line == "-help" || line == "-commands") {
				context.Channel.SendMessageAsync(
					"`cpbot/cpb/-/bot` \nprefisso per interagire con Cpbot\n" +
					"`cpbot msg` \nper interagire col bot\n" +
					"`cpbot -x` \ndove x è un comando\n\n" + 
					"**Comandi  [uno dei prefissi bot] -x **\n" + 
					"`debug: abilita la debug mode dove verranno esplicati i dettagli\n" +
					"quit: salva la conversazione fino a quel momento\n" +
					"save: lo stesso di quit\n" +
					"sim: elenca le frasi simili rispetto all ultimo messaggio\n" +
					"simw: stabilisce un treshold per trovare le frasi simili (-simw [5 to 40])\n" +
					"last: riscrive l'ultimo messaggio\n" +
					"count: mostra il count di tutte le frasi salvate\n" +
					"ping: mention\n" +
					"remove [entry]: rimuove una entry e le risposte dalle frasi conosciute" +
					"youtube [title]: effettua una ricerca su youtube" +
					"avatar [nome]: ritorna il png dell avatar [nome opzionale]`"
					);
				return true;
			}
			else if (line.Contains("-avatar")) {
				string a = line.Substring(line.IndexOf("-avatar") + "-avatar".Length);
				if (a.Length > 2) {
					//context.Channel.SendMessageAsync(a);
					var usersCollection = client.GetChannel(context.Channel.Id).Users;
					List<SocketUser> users = new List<SocketUser>(usersCollection);
					foreach (SocketUser user in users) { if (user.Username.ToLower().Contains(a.Trim(' ')) || user.Mention.ToLower().Contains(a.Trim(' '))) context.Channel.SendMessageAsync(user.GetAvatarUrl()); }
				}
				else {
					context.Channel.SendMessageAsync(context.User.GetAvatarUrl());
				}
				return true;
			}

			return false;
		}
		private void ReactionsAuto(SocketUserMessage msg, SocketGuildChannel guild) {
			string m = msg.ToString().ToLower();
			Random r = new Random();
			IReadOnlyCollection<GuildEmote> em = guild.Guild.Emotes;
			emotes = new List<GuildEmote>(em);
			GuildEmote emo = emotes[0];

			if (brain.Contains(m, "tempo", "devati")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "perderetempo") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
			else if (brain.Contains(m, "wow", "roasted", "epico", "figo")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "wow") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
			else if (brain.Contains(m, "freank", "gamejolt", "indiexpo")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "freank") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
			else if (brain.Contains(m, "zeb", "zerbino")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "ddd") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
			else if (brain.Contains(m, "15e18", "15 e 18", "quanto fa")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "15e18") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
			else if (brain.Contains(m, "mdd", "madonna de dio")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "mdd") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
			else if (brain.Contains(m, "guru", "unity è", "gameguru")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "guru") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
			else if (brain.Contains(m, "rip")) {
				foreach (GuildEmote e in emotes) { if (e.Name.ToLower() == "tobey") { emo = e; break; } }
				msg.AddReactionAsync(emo);
			}
		}



		

	}
}
