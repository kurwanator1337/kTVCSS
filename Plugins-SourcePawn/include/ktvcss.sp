#define PLUGIN_NAME           "kTVCSS adds"
#define PLUGIN_AUTHOR         "Rurix"
#define PLUGIN_VERSION        "1.1"

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
}

// Создаем меню cm
public Action:CancelMatch(client, args)
{
    if (IsVoteInProgress())
    {
        return
    }
    Menu menu = new Menu(Handle_VoteMenu);
    menu.SetTitle("Отменить матч?");
    menu.AddItem("Да", "Да");
    menu.AddItem("Нет", "Нет");
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
    menu.SetTitle("Отменить половину?");
    menu.AddItem("Да", "Да");
    menu.AddItem("Нет", "Нет");
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
        	CPrintToChatAll("{fullred}Голосование провалилось");
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
		CPrintToChat(client, "{fullred}Смена сторон заблокирована!");
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
	change.SetTitle("Сменить сторону?");
	change.AddItem("Да", "Да");
	change.AddItem("Нет", "Нет");
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
