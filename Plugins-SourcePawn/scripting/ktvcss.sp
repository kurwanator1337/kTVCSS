#define PLUGIN_NAME           "kTVCSS adds"
#define PLUGIN_AUTHOR         "Rurix"
#define PLUGIN_VERSION        "1.3"

#include <sourcemod>
#include <sdktools>
#include <cstrike>
#include <topmenus>
#include <clientmod>
#include <clientmod/multicolors>

public Plugin:myinfo =
{
	name = PLUGIN_NAME,
	author = PLUGIN_AUTHOR,
	version = PLUGIN_VERSION,
};



// Глобальные переменные
int voteCount = 0;
new g_votetype = 0;
ConVar:isMatch = null;
Handle:Pause_Max = null;
int grenadecount[MAXPLAYERS][3];
// Starts on plugin start
public void OnPluginStart()
{
	//Айдишники с командами
	RegConsoleCmd("sm_usrlst", userlist)
	//Деньги
	RegConsoleCmd("sm_cash", cash);
	HookEvent("round_start", Event_CashToChat);
	// Отмена половины и матча
	RegConsoleCmd("sm_cm", CancelMatch);
	RegConsoleCmd("sm_nl", NotLive);
	//Смена сторон
	RegConsoleCmd("jointeam", ChooseTeam);
	RegConsoleCmd("spectate", ChooseTeam);
	isMatch = CreateConVar("isMatch", "0");
	//Голосование за сторону
	RegAdminCmd("ChangeVote", ChangeVote, ADMFLAG_ROOT);
	//Пауза в 300 сек
	Pause_Max = FindConVar("mp_freezetime");
	SetConVarBounds(Pause_Max, ConVarBound_Upper, true, 300.0);
	//Создание новых команд
	RegAdminCmd("sys_say", SYS_Say, ADMFLAG_ROOT);
	//Фиксация состояния матча
	RegAdminCmd("save_match", Save_Match, ADMFLAG_ROOT);
	//Восстановление матча
	RegAdminCmd("match_recover", match_recover, ADMFLAG_ROOT);
	RegAdminCmd("score_set", score_set, ADMFLAG_ROOT);
	RegAdminCmd("player_score_set", player_score_set, ADMFLAG_ROOT);
	//Фикс очков с бомбы и дефа
	HookEvent("bomb_defused", def_score);
	HookEvent("bomb_exploded", expl_score);
	//Запрет покупки гранат
	HookEvent("round_start", grenadecounter_null);
}

// Создаем меню cm
public Action:CancelMatch(client, args)
{
    if (IsVoteInProgress())
    {
        return
    }
    Menu menu = new Menu(Handle_VoteMenu);
    menu.SetTitle("Cancel match?");
    menu.AddItem("Да", "Yes");
    menu.AddItem("Нет", "No");
    menu.ExitButton = false;
    menu.DisplayVoteToAll(20);
    g_votetype = 1;
}

// Создаем меню nl
public Action:NotLive(client, args)
{
    if (IsVoteInProgress())
    {
        return
    }
    Menu menu = new Menu(Handle_VoteMenu);
    menu.SetTitle("Cancel half?");
    menu.AddItem("Да", "Yes");
    menu.AddItem("Нет", "No");
    menu.ExitButton = false;
    menu.DisplayVoteToAll(20);
    g_votetype = 2;
}

// Создаем handle для голосования
public int Handle_VoteMenu(Menu menu, MenuAction action, int param1, int param2)
{
	if (action == MenuAction_End)
    {
        delete menu
    }
	else if (action == MenuAction_Select)
    {
    	if (param2 == 0)
    	{
    		voteCount++;
    		//PrintToChatAll("Хуйня отработала");
    	}
    }
	if (action == MenuAction_VoteEnd)
	{
		int clientCount = GetClientCount() - 1;
		//PrintToChatAll("%i", voteCount);
		//PrintToChatAll("%i", clientCount);
		//PrintToChatAll("%f", float(voteCount) / float(clientCount));
		if ((float(voteCount) / float(clientCount)) > 0.6)
		{
			if (g_votetype == 1)
			{
				LogToGame("World triggered CancelMatch");
				//PrintToChatAll("%i", voteCount);
			}
			else
			{
				LogToGame("World triggered NotLive");
				//PrintToChatAll("%i", voteCount);
			}
			voteCount = 0;
		}
		else
		{
        	CPrintToChatAll("{fullred}The vote failed");
        	//PrintToChatAll("%i", voteCount);
        	voteCount = 0;
        }
	}
}

//Айдишники с тимами
public Action:userlist(client, args)
{
	for (new i = 1; i <= MaxClients; i++)
	{
		if (IsClientInGame(i) && !IsFakeClient(i))
		{
			new String:sName[32];
			GetClientName(i, sName, sizeof(sName)-1);
			new t = GetClientTeam(i);
			PrintToServer("%s\t%i", sName, t);
		}
	}
}

//Вывод денег в чат
public void Event_CashToChat(Event hEvent, const char[] sEvName, bool bDontBroadcast)
{
	for (new i = 1; i <= MaxClients; i++)
	{
		if (IsClientInGame(i) && !IsFakeClient(i))
		{
			for (new j = 1; j <= MaxClients; j++)
			{
				if (IsClientInGame(j) && !IsFakeClient(j))
				{
					if (GetClientTeam(i) == GetClientTeam(j))
					{
						CPrintToChat(i, "{fullred}%N ===> $%i", j, GetEntProp(j, Prop_Send, "m_iAccount"));
					}
				}
			}
		}
	}
}

//Вывод денег в консоль
public Action:cash(client, args)
{
	for (new i = 1; i <= MaxClients; i++)
	{
		if (IsClientInGame(i) && !IsFakeClient(i))
		{
			new String:StId[64];
			GetClientAuthId(i, AuthId_Steam2, StId, sizeof(StId)); 
			PrintToServer("%s;%i", StId, GetEntProp(client, Prop_Send, "m_iAccount"));
		}
	}
}

//Смена сторон
public Action:ChooseTeam(client, args)
{
	if (client == 0)
	{
		return Plugin_Continue;
	}
	
	if (GetConVarBool(isMatch) && GetClientTeam(client) > 1)
	{
		CPrintToChat(client, "{fullred}Side switching blocked!");
		return Plugin_Stop;
	}
	return Plugin_Continue;
}



public Action:ChangeVote(int client, int args)
{
	
	if (IsVoteInProgress() && client != 0)
    {
    	return;
   	}
	Menu change = new Menu(Handle_ChangeVote);
	change.SetTitle("Change sides?");
	change.AddItem("Да", "Yes");
	change.AddItem("Нет", "No");
	change.ExitButton = false;
	char tempbuff[2];
	GetCmdArg(1, tempbuff, sizeof(tempbuff))
	for (new i = 1; i <= MaxClients; i++)
	{
		if (IsClientInGame(i) && !IsFakeClient(i) && GetClientTeam(i) == StringToInt(tempbuff))
		{
			change.Display(i, 20);
			break;
		}
	}
}


public int Handle_ChangeVote(Menu change, MenuAction action, int param1, int param2)
{
	
	if (action == MenuAction_End)
	{
		if (change != INVALID_HANDLE)
		{
			CloseHandle(change);
		}
	}
	else if (action == MenuAction_Select)
	{
		if (param2 == 0)
		{
			LogToGame("KTV_CHANGE_SIDES");
		}
		else 
		{
			LogToGame("KTV_DONTCHANGE_SIDES");
		}
	}
	CloseHandle(change);
}

//sys_say {green} Текст		 https://www.doctormckay.com/morecolors.php
public Action SYS_Say(int client, int args)
{
    if (client == 0)
    {
        char l_buffer[1024];
        GetCmdArgString(l_buffer, sizeof(l_buffer));
        ReplaceString(l_buffer, sizeof(l_buffer), "{ ", "{", true);
        ReplaceString(l_buffer, sizeof(l_buffer), " }", "}", true);
        char l_MSG[1024];
    	Format(l_MSG, sizeof(l_MSG), "{fullred}[kTVCSS] %s", l_buffer);
    	//CFormatColor(l_MSG, sizeof(l_MSG), 0);
    	
    	CPrintToChatAll("%s", l_MSG);
    }
    else if (IsClientInGame(client) && !IsFakeClient(client))
    {
        PrintToConsole(client, "[kTVCSS] This command can only be used by the server!");
    }
    
    return Plugin_Handled;
}

//Сохранение состояние матча
public Action Save_Match(int client, int args)
{
	if (client == 0)
	{
		for (new i = 1; i <= MaxClients; i++)
		{
			if (IsClientInGame(i) && !IsFakeClient(i) && GetClientTeam(i) > 1) 
			{
				char StId[64];
				GetClientAuthId(i, AuthId_Steam2, StId, sizeof(StId));
				ReplaceString(StId, sizeof(StId), ":", "|", true);
				char ent_pri_classname[64] = "-1";
				if (GetPlayerWeaponSlot(i, CS_SLOT_PRIMARY) != -1) 
				{
					GetEntityClassname(GetPlayerWeaponSlot(i, CS_SLOT_PRIMARY), ent_pri_classname, sizeof(ent_pri_classname));
				}
				char ent_sec_classname[64] = "-1";
				if (GetPlayerWeaponSlot(i, CS_SLOT_SECONDARY) != -1) 
				{
					GetEntityClassname(GetPlayerWeaponSlot(i, CS_SLOT_SECONDARY), ent_sec_classname, sizeof(ent_sec_classname));
				}
				LogToGame("[KBACKUP] {%s};{%i};{%s};{%s};{%i, %i, %i};{%i, %i};{%i};{%i, %i}", //[KBACKUP] {STEAM};{Money};{Pri};{Sec};{He, Fl, Sm};{Helm, Arm};{Def};{Kill, Death}
				StId, //1
				GetEntProp(i, Prop_Send, "m_iAccount"), //2
				ent_pri_classname,//3
				ent_sec_classname, //4
				GetEntProp(i, Prop_Send, "m_iAmmo", _, 11), //5
				GetEntProp(i, Prop_Send, "m_iAmmo", _, 12), //5
				GetEntProp(i, Prop_Send, "m_iAmmo", _, 13), //5
				GetEntProp(i, Prop_Send, "m_bHasHelmet"), //6
				GetEntProp(i, Prop_Send, "m_ArmorValue"), //6
				GetEntProp(i, Prop_Send, "m_bHasDefuser"), //7
				GetClientFrags(i), //8
				GetClientDeaths(i) //8
				);
			}
		}
		LogToGame("KTV_CT %i KTV_T %i", GetTeamScore(CS_TEAM_CT), GetTeamScore(CS_TEAM_T));
	}
	else if (IsClientInGame(client) && !IsFakeClient(client))
	{
		PrintToConsole(client, "[kTVCSS] This command can only be used by the server!");
	}
	return Plugin_Handled;
}

//Восстановление состояния матча

public Action match_recover(int client, int args)
{
	if (client == 0)
	{
		char Arg_Auth[64];
		char Arg_Money[64];
		char Arg_Primary[64];
		char Arg_Secondary[64];
		char Arg_HE[64];
		char Arg_Flash[64];
		char Arg_Smoke[64];
		char Arg_Helmet[64];
		char Arg_Armor[64];
		char Arg_Defuse[64];
		char Arg_Frags[64];
		char Arg_Deaths[64];
		
		GetCmdArg(1, Arg_Auth, sizeof(Arg_Auth));
		GetCmdArg(2, Arg_Money, sizeof(Arg_Money));
		GetCmdArg(3, Arg_Primary, sizeof(Arg_Primary));
		GetCmdArg(4, Arg_Secondary, sizeof(Arg_Secondary));
		GetCmdArg(5, Arg_HE, sizeof(Arg_HE));
		GetCmdArg(6, Arg_Flash, sizeof(Arg_Flash));
		GetCmdArg(7, Arg_Smoke, sizeof(Arg_Smoke));
		GetCmdArg(8, Arg_Helmet, sizeof(Arg_Helmet));
		GetCmdArg(9, Arg_Armor, sizeof(Arg_Armor));
		GetCmdArg(10, Arg_Defuse, sizeof(Arg_Defuse));
		GetCmdArg(11, Arg_Frags, sizeof(Arg_Frags));
		GetCmdArg(12, Arg_Deaths, sizeof(Arg_Deaths));
		
		int i_client = -1;
		ReplaceString(Arg_Auth, sizeof(Arg_Auth), "|", ":", true);
		for (new k_client = 1; k_client < MAXPLAYERS; k_client++)
		{
			if (IsClientInGame(k_client) && !IsFakeClient(k_client) && GetClientTeam(k_client) > 1)
			{
				char StId[64];
				GetClientAuthId(k_client, AuthId_Steam2, StId, sizeof(StId));
				if (StrEqual(StId, Arg_Auth, false))
				{
					i_client = k_client;
					break;
				}
			}
		}
		
		if (IsClientInGame(i_client) && !IsFakeClient(i_client) && GetClientTeam(i_client) > 1)
		{
			//LogToGame("[KBACKUP] {%s};{%i};{%i};{%i};{%i, %i, %i};{%i, %i};{%i};{%i, %i}",
			
			if (GetPlayerWeaponSlot(i_client, CS_SLOT_PRIMARY) != -1)
			{
				RemovePlayerItem(i_client, GetPlayerWeaponSlot(i_client, CS_SLOT_PRIMARY));
			}
			
			if (GetPlayerWeaponSlot(i_client, CS_SLOT_SECONDARY) != -1)
			{
				RemovePlayerItem(i_client, GetPlayerWeaponSlot(i_client, CS_SLOT_SECONDARY));
			}
			
			SetEntProp(i_client, Prop_Send, "m_iAccount", StringToInt(Arg_Money)); //2
			
			if (StringToInt(Arg_Primary) != -1) //3
			{
				GivePlayerItem(i_client, Arg_Primary); 
			}
			if (StringToInt(Arg_Secondary) != -1) //4
			{
				GivePlayerItem(i_client, Arg_Secondary); 
			}
			if (StringToInt(Arg_HE) != 0) //5
			{
				GivePlayerItem(i_client, "weapon_hegrenade"); 
			}
			if (StringToInt(Arg_Flash) != 0) //5
			{
				if (StringToInt(Arg_Flash) > 1)
				{
					GivePlayerItem(i_client, "weapon_flashbang");
					GivePlayerItem(i_client, "weapon_flashbang");
				}
				else 
					GivePlayerItem(i_client, "weapon_flashbang");
			}
			if (StringToInt(Arg_Smoke) != 0) //5
			{
				GivePlayerItem(i_client, "weapon_smokegrenade"); 
			}
			if (StringToInt(Arg_Armor) != 0) //6
			{
				if (StringToInt(Arg_Helmet) != 0) 
				{
					GivePlayerItem(i_client, "item_assaultsuit"); 
				}
				else
					GivePlayerItem(i_client, "item_kevlar");
					
				SetEntProp(i_client, Prop_Send, "m_ArmorValue", StringToInt(Arg_Armor));
			}
			if (StringToInt(Arg_Defuse) != 0) //7
			{
				GivePlayerItem(i_client, "item_defuser"); 
			}
			SetEntProp(i_client, Prop_Data, "m_iFrags", StringToInt(Arg_Frags)); //8
			SetEntProp(i_client, Prop_Data, "m_iDeaths", StringToInt(Arg_Deaths)); //8
		}
	}
	else if (IsClientInGame(client) && !IsFakeClient(client))
	{
		PrintToConsole(client, "[kTVCSS] This command can only be used by the server!");
	}
	return Plugin_Handled;
}

public Action player_score_set(int client, int args)
{
    if (client == 0)
    {
    	char Arg_Auth[64];
    	char Arg_Frags[64];
    	char Arg_Deaths[64];
    	
    	GetCmdArg(1, Arg_Auth, sizeof(Arg_Auth));
    	GetCmdArg(2, Arg_Frags, sizeof(Arg_Frags));
    	GetCmdArg(3, Arg_Deaths, sizeof(Arg_Deaths));
    	
    	int i_client = -1;
    	ReplaceString(Arg_Auth, sizeof(Arg_Auth), "|", ":", true);
    	for (new k_client = 1; k_client < MAXPLAYERS; k_client++)
    	{
			if (IsClientInGame(k_client) && !IsFakeClient(k_client) && GetClientTeam(k_client) > 1)
			{
				char StId[64];
				GetClientAuthId(k_client, AuthId_Steam2, StId, sizeof(StId));
				if (StrEqual(StId, Arg_Auth, false))
    			{
    				i_client = k_client;
    				break;
    			}
			}
    	}
    	if (IsClientInGame(i_client) && !IsFakeClient(i_client) && GetClientTeam(i_client) > 1) 
		{
			SetEntProp(i_client, Prop_Data, "m_iFrags", StringToInt(Arg_Frags)); //8
			SetEntProp(i_client, Prop_Data, "m_iDeaths", StringToInt(Arg_Deaths)); //8
		}
	}
	else if (IsClientInGame(client) && !IsFakeClient(client))
    {
        PrintToConsole(client, "[kTVCSS] This command can only be used by the server!");
    }
    return Plugin_Handled;
}

public Action score_set (int client, int args) 
{
	if (client == 0)
    {
    	char CT_Score[64];
    	char T_Score[64];
    	
    	GetCmdArg(1, CT_Score, sizeof(CT_Score));
    	GetCmdArg(2, T_Score, sizeof(T_Score));

    	SetTeamScore(3, StringToInt(CT_Score));
    	SetTeamScore(2, StringToInt(T_Score));
	}
	else if (IsClientInGame(client) && !IsFakeClient(client))
	{
		PrintToConsole(client, "[kTVCSS] This command can only be used by the server!");
	}
	return Plugin_Handled;
}

//Фикс очков с бомбы и дефа
public void def_score(Event event, const char[] name, bool dontBroadcast)
{
	int client = GetClientOfUserId(GetEventInt(event, "userid"));
	SetEntProp(client, Prop_Data, "m_iFrags", GetEntProp(client, Prop_Data, "m_iFrags") - 3);
}

public void expl_score(Event event, const char[] name, bool dontBroadcast)
{
	int client = GetClientOfUserId(GetEventInt(event, "userid"));
	SetEntProp(client, Prop_Data, "m_iFrags", GetEntProp(client, Prop_Data, "m_iFrags") - 3);
}

public void grenadecounter_null(Event hEvent, const char[] sEvName, bool bDontBroadcast) 
{
	for (new i = 1; i < MAXPLAYERS; i++) 
	{
		if (IsClientInGame(i) && !IsFakeClient(i))
		{
			grenadecount[i][0] = GetEntProp(i, Prop_Send, "m_iAmmo", _, 11);
			grenadecount[i][1] = GetEntProp(i, Prop_Send, "m_iAmmo", _, 12);
			grenadecount[i][2] = GetEntProp(i, Prop_Send, "m_iAmmo", _, 13);
		}
	}
}

public Action CS_OnBuyCommand(int client, const char[] weapon)
{
	if (!GetConVarBool(isMatch) || (client == 0))
	{
		return Plugin_Continue;
	}

	if (StrEqual(weapon, "nvgs", false))
	{
		CPrintToChat(client, "{fullred}[kTVCSS] {white}Nightvision Blocked!");
		return Plugin_Handled;
	}
	
	if (GetConVarBool(isMatch))
	{
		new String:the_weapon[32];
		Format(the_weapon, sizeof(the_weapon), "%s", weapon);
		ReplaceString(the_weapon, sizeof(the_weapon), "weapon_", "");
		ReplaceString(the_weapon, sizeof(the_weapon), "item_", "");
		if (IsClientInGame(client) && !IsFakeClient(client)) 
		{
			if (StrEqual(the_weapon, "hegrenade", false)) 
			{
				if (grenadecount[client][0] < 1)
				{
					grenadecount[client][0]++;
					return Plugin_Continue;
				}
				else 
					return Plugin_Handled;
			}
			else if (StrEqual(the_weapon, "flashbang", false)) 
			{
				if (grenadecount[client][1] < 2)
				{
					grenadecount[client][1]++;
					return Plugin_Continue;
				}
				else 
					return Plugin_Handled;
			}
			else if (StrEqual(the_weapon, "smokegrenade", false)) 
			{
				if (grenadecount[client][2] < 1)
				{
					grenadecount[client][2]++;
					return Plugin_Continue;
				}
				else 
					return Plugin_Handled;
			}
		}
	}
	return Plugin_Continue;
}
