using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Rhythm {

    public enum NoteType {
        Normal = 1, HoldStart, HoldEnd
    };

    public class Note {
        public int laneIndex;
        public bool isJudged;
        public int timeMs; //MSec
        public float timeMeasure;
        public NoteType type;

        public Note (int laneIndex, float timeMeasure, int timeMs, NoteType type){
            this.laneIndex = laneIndex;
            this.timeMeasure = timeMeasure;
            this.timeMs = timeMs;
            this.type = type;
            isJudged = false;
        }
    }

    public class NoteHoldStart : Note {
        public Note holdEnd;
        public NoteHoldStart(int laneIndex, float timeMeasure, int timeMs, NoteType type) : base(laneIndex, timeMeasure, timeMs, type) { }
    }

    /// <summary>
    /// 譜面データクラス。タイトル情報からノーツデータまで。
    /// </summary>
    public class Notes {

        private string fileContent;
        public string songTitle;
        public string songArtist;
        public float songPreviewStartSec; //Sec
        public float songPreviewDurationSec; //Sec
        public float songOffsetMSec; //MSec
        public float[,] songBPMs; //[index, info(Measure, BPM)]
        public float[,] songStops;
        public string songNotes;

        public List<Note> notes = new List<Note>();

        public bool isCompleteLoad = false;

        /// <summary>
        /// 譜面データクラスのコンストラクタ。譜面データのメタデータ解析をする。
        /// </summary>
        /// <param name="path">譜面ファイルのパス</param>
        public Notes(string path) {

            //読み込み
            fileContent = Resources.Load<TextAsset>(path).text;
            //整形(不要かも)
            fileContent = fileContent.Replace(" ", "").Replace("　", "");
            fileContent = Regex.Replace(fileContent, "(?<=\n)\n", ""); //空白行の削除(最初の行以外)
            fileContent = Regex.Replace(fileContent, "^\n", ""); //空白行の削除(最初の行)

            //楽曲情報の取得
            songTitle = Regex.Match(fileContent, "(?<=#TITLE:)([\\s\\S]*?)(?=;)").Value.Replace("\n", "");
            songArtist = Regex.Match(fileContent, "(?<=#ARTIST:)([\\s\\S]*?)(?=;)").Value.Replace("\n", "");
            float.TryParse(Regex.Match(fileContent, "(?<=#SAMPLESTART:)([\\s\\S]*?)(?=;)").Value.Replace("\n", ""), out songPreviewStartSec);
            float.TryParse(Regex.Match(fileContent, "(?<=#SAMPLELENGTH:)([\\s\\S]*?)(?=;)").Value.Replace("\n", ""), out songPreviewDurationSec);
            float.TryParse(Regex.Match(fileContent, "(?<=#OFFSET:)([\\s\\S]*?)(?=;)").Value.Replace("\n", ""), out songOffsetMSec);
            songOffsetMSec *= 1000f; //Sec -> MSec

        }

        public IEnumerator LoadNotes() {

            //BPM変化の取得 
            string[] tmpBPMArray = (Regex.Match(fileContent, "(?<=#BPMS:)([\\s\\S]*?)(?=;)").Value.Replace("\n", "")).Split(',');
            songBPMs = new float[tmpBPMArray.Length, 2];
            for (int i = 0; i < tmpBPMArray.Length; i++) {
                float.TryParse(Regex.Match(fileContent, "([0-9.]*)(?==)").Value, out songBPMs[i, 0]);
                songBPMs[i, 0] /= 4f;
                float.TryParse(Regex.Match(fileContent, "(?<==)([0-9.]*)").Value, out songBPMs[i, 1]);
                yield return null;

                // TODO : BPM変化・停止のindexは小節数ではなく拍数、コード書く前に調べろ
            }

            //停止情報の取得
            string tmpStopData = (Regex.Match(fileContent, "(?<=#STOPS:)([\\s\\S]*?)(?=;)").Value.Replace("\n", ""));
            if (string.Empty != tmpStopData) {
                string[] tmpStopArray = tmpStopData.Split(',');
                for (int i = 0; i < tmpStopArray.Length; i++) {
                    float.TryParse(Regex.Match(fileContent, "([0-9.]*)(?==)").Value, out songStops[i, 0]);
                    songStops[i, 0] /= 4f;
                    float.TryParse(Regex.Match(fileContent, "(?<==)([0-9.]*)").Value, out songStops[i, 1]);
                    yield return null;
                }
            }

            //ノーツ情報のみ抽出
            songNotes = Regex.Match(fileContent, "(?<=#NOTES:)([\\s\\S]*?)(?=;)").Value;
            songNotes = Regex.Replace(songNotes, "//\\w*(?=\n)", ""); //コメントの削除
            songNotes = Regex.Replace(songNotes, "(?<=\n)([\\w-.,]*):\n", ""); //譜面のメタデータ削除
            songNotes = Regex.Replace(songNotes, "(?<=\n)\n", ""); //空白行の削除(最初の行以外)
            songNotes = Regex.Replace(songNotes, "^\n", ""); //空白行の削除(最初の行)、コメント削除などによって冒頭行が空行になるかもなので最後に実行する
            // TODO : \n,\nでSplitしろ
            songNotes = Regex.Replace(songNotes, "\n,\n", ","); //,でスプリットするときいい感じにするため 

            //ノーツ解析
            float totalMs = 0; //直前の小節までの経過時間

            string[] tmpNoteMeasures = songNotes.Split(',');
            for (int i_measure = 0; i_measure < tmpNoteMeasures.Length; i_measure++) {

                //ノーツ解析
                string[] tmpNotesRows = tmpNoteMeasures[i_measure].Split('\n');
                for (int i_row = 0; i_row < tmpNotesRows.Length; i_row++) {
                    string tmpRow = tmpNotesRows[i_row];
                    for (int i_lane = 0; i_lane < tmpRow.Length; i_lane++) {
                        char tmpLaneInfo = tmpRow[i_lane];
                        if (tmpLaneInfo == '0' || i_lane >= Lane.LaneMax) {
                            continue;
                        }
                        //小節数の計算(小節内における場所、0~1)
                        float measure = (float)i_row / tmpNotesRows.Length;

                        //ノートインスタンスの作成
                        NoteType type = (NoteType)(int.Parse(tmpLaneInfo.ToString()));
                        Note note = new Note(i_lane, i_measure + measure, (int)songOffsetMSec + (int)totalMs + GetMeasureLength(i_measure, measure), type);

                        // TODO : ホールド時の分岐、キャストする

                        notes.Add(note);
                    }
                }
                yield return null;

                //現在の小節の秒数を加算
                totalMs += GetMeasureLength(i_measure, 1);
            }

            isCompleteLoad = true;
        }

        /// <summary>
        /// 特定の小節内における、指定部分までのミリ秒を計算して返します
        /// </summary>
        /// <returns>指定小節内の指定部分までのミリ秒</returns>
        /// <param name="currentMeasureStart">小節番号</param>
        /// <param name="currentMeasurePos">小節番号(端数0~1)</param>
        int GetMeasureLength(int currentMeasureStart, float currentMeasurePos) {
            //BPM位置の算出
            int BPMindex = 0;
            for (; ; ) {
                if (BPMindex == (songBPMs.GetLength(0) - 1)) {
                    break;
                } else if (songBPMs[BPMindex + 1, 0] > currentMeasureStart) {
                    break;
                } else if (songBPMs[BPMindex + 1, 0] <= currentMeasureStart) {
                    BPMindex++;
                }
            }

            float res = 0;
            if (BPMindex == (songBPMs.GetLength(0) - 1)) {
                res = (60f / songBPMs[BPMindex, 1]) * 1000 * 4 * currentMeasurePos;
            } else if (songBPMs[BPMindex + 1, 0] > currentMeasurePos) {
                res = (60f / songBPMs[BPMindex, 1]) * 1000 * 4 * currentMeasurePos;
            } else {
                //TODO : たぶんここ計算式間違ってるのでデバッグして

                res = (60f / songBPMs[BPMindex, 1]) * 1000 * 4 * (songBPMs[BPMindex + 1, 1] - currentMeasureStart)
                    + (60f / songBPMs[BPMindex + 1, 1]) * 1000 * 4 * (1 - (songBPMs[BPMindex + 1, 1] - (currentMeasureStart + currentMeasurePos)));
            }
            return (int)res;
        }
    }
}