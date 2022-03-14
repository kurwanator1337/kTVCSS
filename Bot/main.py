import vk_api
from vk_api.longpoll import VkLongPoll, VkEventType
from datetime import datetime
from core.db.stats import Db
import re

admins = []

class Bot:

    def __init__(self, api_token):
        vk_session = vk_api.VkApi(token=api_token)
        self.longpoll = VkLongPoll(vk_session)
        self.vk = vk_session.get_api()
        self.event = None
        print("Started kTVCSS-Ranking-System-Bot")

    def _send_message(self, message):
        self.vk.messages.send(message=message, user_id=self.event.user_id, random_id=self.random_id())

    def _send_mentioned_message(self, message):
        self.vk.messages.send(message=self._get_user_firstname() + ', ' + message, user_id=self.event.user_id, random_id=self.random_id())

    def _send_steam_not_found(self):
        header = f'не найден Steam ID связанный с этой страницей.\n'
        body = f'Привяжите вашу страницу командой !setid ВАШ_STEAM_ID'
        self._send_mentioned_message(header + body)

    def _get_user_firstname(self) -> str:
        return self.vk.users.get(user_ids=self.event.user_id)[0]['first_name']

    def random_id(self):
        """
        Generates RandomID
        """
        dt = datetime.now()
        epoch = datetime.utcfromtimestamp(0)
        return (dt - epoch).total_seconds() * 1000.0

    def resetid(self):
        """
        [ADMIN] Remove VkID from SteamID
        """
        if len(self.event.text.split(' ')) <= 1:
            self._send_message('Недостаточно аргументов.\nПример: !resetid STEAM_0:0:0123456789')
            return None
        try:
            steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
        except Exception:
            self._send_message('Введенный STEAM_ID некорректный.\nПример: !resetid STEAM_0:0:0123456789')
            return None
        db = Db()
        if db.remove_id_by_steam(steamid) == 0:
            print('Admin %s used !resetid for %s' % (str(self.event.user_id), steamid))

    def help(self):
        """
        Displays help message
        """
        self._send_mentioned_message("для вас доступны следующие команды: !help, !setid, !delid, !mystats, !togglestats")
        if str(self.event.user_id) in admins:
            self._send_message("Так как вы администратор, можете сделать ♂fisting♂ через: !resetid, !getinfo")

    def togglestats(self):
        """
        Toggles player's statistics sending
        """
        db = Db()
        result = db.toggle_stats(self.event.user_id)
        if result == -1:
            self._send_steam_not_found()
        else:
            self._send_mentioned_message(f'для вас уведомления были {"выключены 🚫" if result == 0 else "включены ✅"}')

    def delid(self):
        """
        Unlinks player's VkID from SteamID
        """
        db = Db()
        result = db.remove_id(self.event.user_id)
        if result == 0:
            self._send_mentioned_message(f"вы отвязали свою страницу ⚠")
        else:
            self._send_steam_not_found()

    def setid(self):
        """
        Link player's SteamID to VkID
        """
        if len(self.event.text.split(' ')) <= 1:
            self._send_message('Недостаточно аргументов.\nПример: !setid STEAM_0:0:0123456789')
            return None
        try:
            steamid = re.search(r"(!\w+) (STEAM_\d:\d:\d+)", self.event.text).group(2).strip()
        except Exception:
            self._send_message('Введенный STEAM_ID некорректный.\nПример: !setid STEAM_0:0:0123456789')
            return None
        db = Db()
        result = db.set_id(self.event.user_id, steamid)
        if result == 0:
            self._send_message(f"Ваш аккаунт ВК {self.event.user_id} был привязан к {steamid}")
        elif result == 1:
            self._send_message("Этот Steam ID не учитывается статистикой.\nСыграйте хотя бы одну игру.")
        elif result == -1:
            self._send_message("Этот аккаунт VK уже привязан!\nДля перепривязки аккаунта воспользуйтесь !delid")

    def mystats(self):
        """
        Prints player's statistics
        """
        db = Db()
        word = 'матч'
        stats = db.get_stats(self.event.user_id)
        if stats is not None:
            header = f"\"{stats['NAME']}\" ваша статистика 📢\nПоследний матч {str(stats['LASTMATCH']).split('.')[0]}.\n"
            kill_stats = f"Убийств: {int(stats['KILLS'])}. Смертей: {int(stats['DEATHS'])}. Хэдшотов: {int(stats['HEADSHOTS'])}. KDR: {stats['KDR']:.2f}.\n"
            kill_sub_stats = f"HSR: {stats['HSR']:.2f}. AVG: {stats['AVG']:.2f}. K/D: {stats['KDR']:.2f}.\n"
            play_stats = f"Побед: {int(stats['MATCHESWINS'])}. Поражений {int(stats['MATCHESLOOSES'])}. Процент побед: {int(stats['WINRATE'])}%\n"
            if int(stats['MATCHESPLAYED']) >= 10:
                rank_footer = f"Текущий рейтинг: {stats['MMR']}. Позиция в рейтинге: UNDEFINED\nВаш текущий ранк: {stats['RANKNAME']}\n"
            else:
                if int(10 - int(stats['MATCHESPLAYED'])) == 1:
                    word = 'матч'
                elif int(10 - int(stats['MATCHESPLAYED'])) in [2, 3, 4]:
                    word = 'матча'
                elif int(10 - int(stats['MATCHESPLAYED'])) in [5, 6, 7, 8, 9]:
                    word = 'матчей'
                rank_footer = f"Вы проходите калибровку. До конца калибровки {10 - stats['MATCHESPLAYED']} {word}.\n"
            final_message = header + kill_stats + kill_sub_stats + play_stats + rank_footer
            self._send_mentioned_message(final_message)
        else:
            self._send_steam_not_found()

    def start(self):
        for self.event in self.longpoll.listen():
            if self.event.type == VkEventType.MESSAGE_NEW and self.event.to_me and self.event.text:
                if self.event.from_user:
                    if self.event.text == '!help':
                        self.help()
                    elif self.event.text == '!togglestats':
                        self.togglestats()
                    elif self.event.text == '!mystats':
                        self.mystats()
                    elif self.event.text.startswith('!delid'):
                        self.delid()
                    elif self.event.text.startswith('!setid'):
                        self.setid()
                    elif self.event.text.startswith('!resetid') and str(self.event.user_id) in admins:
                        self.resetid()
                    '''
                    elif self.event.text.startswith('!dbg_getinfo') and str(self.event.user_id) in admins:
                        self.getinfo()
                    '''


def main():
    with open('./token', 'r') as token_file:
        token = token_file.readline()
    with open('./admins', 'r') as admins_file:
        for line in admins_file:
            temp = line.split(' ')[0].rstrip()
            if temp != '': admins.append(temp)
    print('Admins loaded:', admins)
    while True:
        try:
            bot = Bot(token)
            bot.start()
            print('[DEBUG] Connected to VK_API!')
        except Exception as e:
            print('[ERROR] EXCEPTION CAUGHT! RESTARTING! ')
            print(e)


if __name__ == '__main__':
    main()
