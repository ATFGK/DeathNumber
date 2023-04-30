using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using System.IO;
using Terraria.DataStructures;
using System.Data;
using TShockAPI.DB;
using MySql.Data.MySqlClient;


namespace TestPlugin
{
    [ApiVersion(2, 1)]//api版本
    public class TestPlugin : TerrariaPlugin
    {
        /// <summary>
        /// Gets the author(s) of this plugin
        /// </summary>
        /// 插件作者
        public override string Author => "GK 阁下";

        /// <summary>
        /// Gets the description of this plugin.
        /// A short, one lined description that tells people what your plugin does.
        /// </summary>
        /// 插件说明
        public override string Description => "统计玩家的死亡次数！";

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        /// 插件名字
        public override string Name => "死亡统计";

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        /// 插件版本
        public override Version Version => new Version(1, 0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the TestPlugin class.
        /// This is where you set the plugin's order and perfrom other constructor logic
        /// </summary>
        /// 插件处理
        public TestPlugin(Main game) : base(game)
        {

        }

        private IDbConnection _数据库;

        /// <summary>
        /// Handles plugin initialization. 
        /// Fired when the server is started and the plugin is being loaded.
        /// You may register hooks, perform loading procedures etc here.
        /// </summary>
        /// 插件启动时，用于初始化各种狗子

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);//钩住游戏初始化时
            ServerApi.Hooks.NetGetData.Register(this, GetData);//钩住收到数据

            _数据库 = TShock.DB;//引用系统数据库
            var _骨架 = new SqlTable("死亡统计",//定义表的骨架
                                     new SqlColumn("用户名", MySqlDbType.VarChar, 32) { Primary = true },//此乃主键
                                     new SqlColumn("死亡数", MySqlDbType.Int32) { DefaultValue = "0" });//此乃默认值
            var _表 = new SqlTableCreator(_数据库, _数据库.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator()
                                                  : new MysqlQueryCreator());//表示如果db的类型为sqlite就启动sqlite否则开mysql
            //问号表示选择，若问号前为真则选择冒号前，否则冒号后
            _表.EnsureTableStructure(_骨架);//确定骨架是否存在，不存在就生成
                                         //_表.GetColumns这是取列
                                         //_表.DeleteRow这是删行



        }

        /// <summary>
        /// Handles plugin disposal logic.
        /// *Supposed* to fire when the server shuts down.
        /// You should deregister hooks and free all resources here.
        /// </summary>
        /// 插件关闭时
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Deregister hooks here
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);//销毁游戏初始化狗子
                ServerApi.Hooks.NetGetData.Deregister(this, GetData);//销毁收到数据狗子

            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args)//游戏初始化的狗子
        {
            //Adding a command is as simple as adding a new ``Command`` object to the ``ChatCommands`` list.
            //The ``Commands` object is available after including TShock in the file (`using TShockAPI;`)
            //第一个是权限，第二个是子程序，第三个是名字

            Commands.ChatCommands.Add(
                new Command("", _指令, "死亡统计")
                {
                    HelpText = "输入/死亡统计 可以显示你的死亡次数"
                });

        }
        private void _指令(CommandArgs args)
        {
            //invert the boolean value


            try
            {

                using (var _表 = _数据库.QueryReader("SELECT * FROM 死亡统计"))
                {

                    while (_表.Read())//判断循环，读一行，读出来了就开始判断
                    {
                        string _用户名 = _表.Get<string>("用户名");
                        int _死亡数 = _表.Get<int>("死亡数");

                        if (args.Player.Name == _用户名)
                        {
                            args.Player.SendSuccessMessage(args.Player.Name + "您累计死亡" + _死亡数.ToString() + "次。");
                            return;
                        }


                    }


                }
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }

            //此时如果没有返回那就表示还没死过，没有创建过表
            args.Player.SendSuccessMessage(args.Player.Name + "您还没有死亡记录哦。");




        }
        private void GetData(GetDataEventArgs _数据内容)//收到数据的狗子程序
        {
            var _用户 = TShock.Players[_数据内容.Msg.whoAmI];

            if (_用户 == null)//不存在
            {
                // _数据内容.Handled = true;//处理完毕好像意义不太大
                return;
            }

            if (!_用户.ConnectionAlive)//丢失连接
            {
                return;
            }

            if (_数据内容.Handled)//若数据处理完毕则返回
            {
                return;
            }

            if (!(_数据内容.MsgID == PacketTypes.PlayerDeathV2))//或许应该提前判断
            {

                return;
            }


            try
            {

                using (BinaryReader _数据 = new BinaryReader(new MemoryStream(_数据内容.Msg.readBuffer, _数据内容.Index, _数据内容.Length)))
                {
                    _数据.ReadByte();//第一个字节是玩家号



                    PlayerDeathReason playerDeathReason = PlayerDeathReason.FromReader(new BinaryReader(_数据.BaseStream));


                    var _伤害 = _数据.ReadInt16();//受到伤害
                    var _方向 = (byte)(_数据.ReadByte() - 1);//名中反向
                    BitsByte bits = (BitsByte)_数据.ReadByte();
                    bool pvp杀 = bits[0];

                    // playerDeathReason.GetHashCode



                    // _数据.Close();


                    if (pvp杀)
                    {

                        //var winner = TShock.Players[playerDeathReason.SourcePlayerIndex];
                        //if (winner != null)
                        //{
                        //    Console.WriteLine(_用户.Name + "被" + winner.Name + "pvp杀了！ ");

                        //}





                        //如果是被pvp杀的不计入死亡记录
                    }
                    else
                    {
                        // Console.WriteLine(_用户.Name + "被怪杀了！ ");


                        int _死亡数 = 0;


                        using (var _表 = _数据库.QueryReader("SELECT * FROM 死亡统计 WHERE 用户名=@0", _用户.Name))
                        {


                            if (_表.Read())//如果读出来了，证明已经添加过了，那就加上死亡数进行更新
                            {
                                _死亡数 = _表.Get<int>("死亡数");
                                //Console.WriteLine(_用户.Name + "死亡数为 "+ _死亡数);
                            }

                        }

                        _死亡数 += 1;//+1
                        //Console.WriteLine(_用户.Name + "加后死亡数为 " + _死亡数);

                        if (_死亡数 == 1)
                        {
                            _数据库.Query("INSERT INTO 死亡统计 (用户名, 死亡数) VALUES (@0, @1);",
                                    _用户.Name, _死亡数);
                            //创建新的行

                        }
                        else
                        {

                            int q = _数据库.Query("UPDATE 死亡统计 SET 死亡数=@0 WHERE 用户名=@1",
                                  _死亡数, _用户.Name);

                            //更新行
                            if (q == 0)//!=表示不等于
                            {

                                Console.WriteLine(_用户.Name + "死亡统计数据更新失败！");
                            }
                        }





                    }



                }



            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError(ex.ToString());
            }







        }





    }

}
