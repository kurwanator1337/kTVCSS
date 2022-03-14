import pymssql
import os


def _read_db_conf():
    path = os.path.join(os.getcwd(), 'db')
    with open(path, 'r') as db_conf:
        conf = db_conf.readline()
    return conf.split('\t')


class Db:
    def __init__(self):
        credentials = _read_db_conf()
        self.mydb = pymssql.connect(server=credentials[0], user=credentials[1], password=credentials[2],
                                    database=credentials[3], as_dict=True)
        self.mycursor = self.mydb.cursor()

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()

    def __del__(self):
        self.close()

    def _check_if_steam_exists(self, steam_id) -> bool:
        self.mycursor.execute(f"SELECT [ID] FROM [dbo].[Players] WHERE [STEAMID]='{str(steam_id)}';")
        result = self.mycursor.fetchone()
        return True if result is not None else False

    def _check_if_id_exists(self, vk_id) -> bool:
        self.mycursor.execute(f"SELECT [ID] FROM [dbo].[Players] WHERE [VKID]={str(vk_id)};")
        result = self.mycursor.fetchone()
        return True if result is not None else False

    def toggle_stats(self, vk_id) -> int:
        # if vk_id.is_integer():
        self.mycursor.execute(f"SELECT [ANOUNCE], [STEAMID] FROM [dbo].[Players] WHERE [VKID]={str(vk_id)};")
        result = self.mycursor.fetchone()
        if result is not None:
            statue = 0 if result['ANOUNCE'] is None else result['ANOUNCE']
            self.mycursor.execute(f"UPDATE [dbo].[Players] SET [ANOUNCE]='{int(statue) ^ 1}' WHERE [VKID]={vk_id};")
            self.mydb.commit()
            return int(statue) ^ 1
        return -1

    def get_stats(self, vk_id) -> dict:
        self.mycursor.execute("SELECT [NAME], [STEAMID], [KILLS], [DEATHS], [HEADSHOTS], [KDR], [HSR], [MMR], [AVG], "
                              "[RANKNAME], [MATCHESPLAYED], [MATCHESWINS], [MATCHESLOOSES], [ISCALIBRATION], "
                              f"[WINRATE], [LASTMATCH] FROM [dbo].[Players] WHERE [VKID]={str(vk_id)};")
        result = self.mycursor.fetchone()
        return result

    def remove_id_by_steam(self, steam_id):
        if self._check_if_steam_exists(steam_id):
            self.mycursor.execute(f"UPDATE [dbo].[Players] SET [VKID]='' WHERE [STEAMID]='{steam_id}';")
            self.mydb.commit()
            return 0
        return -1

    def remove_id(self, vk_id) -> int:
        if self._check_if_id_exists(vk_id):
            self.mycursor.execute(f"UPDATE [dbo].[Players] SET [VKID]='' WHERE [VKID]={vk_id};")
            self.mydb.commit()
            return 0
        return -1

    def set_id(self, vk_id, steam_id) -> int:
        if not self._check_if_id_exists(vk_id):
            if self._check_if_steam_exists(steam_id):
                self.mycursor.execute(f"UPDATE [dbo].[Players] SET [VKID]={vk_id} WHERE [STEAMID]='{steam_id}';")
                self.mydb.commit()
                return 0
            return 1
        return -1

    def close(self):
        #print('[DB] Closed!')
        self.mydb.close()


if __name__ == '__main__':
    db_inst = Db()
    ex = db_inst._check_if_id_exists('459551868')
    print(ex)
