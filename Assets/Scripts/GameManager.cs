using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rhythm;

public class GameManager : MonoBehaviour {

    public struct NoteObject {
        public Note note;
        public GameObject gameObject;

        public NoteObject(Note note, GameObject gameObject) {
            this.note = note;
            this.gameObject = gameObject;
        }
    }

    Notes notes;
    Lane lane;
    List<NoteObject> noteObjects = new List<NoteObject>(); //画面上に表示する
    NotesPool notesPool;

    int displayIndex = 0;
    int judgedIndex = 0;
    bool isLoadedMusic = false;
    bool isSongPlayCompleted = false;
    AudioSource audioSource;
    int currentMusicTimeMSec = 0;

    [SerializeField] Sprite[] sprites = new Sprite[5];

    void Start() {
        notes = new Notes("VsInvader/easy");
        lane = new Lane();
        StartCoroutine(notes.LoadNotes());
        notesPool = GetComponent<NotesPool>();


    }

    void Update() {
        //読み込み終了判定
        if (!notes.isCompleteLoad) {
            return;
        }
        //楽曲読み込み終了判定
        if (!isLoadedMusic) {
            audioSource = GetComponent<AudioSource>();
            audioSource.clip = Resources.Load<AudioClip>("VsInvader/song");
            audioSource.Play();
            isLoadedMusic = true;
        }
        if (audioSource != null){
            currentMusicTimeMSec = (int)(audioSource.time * 1000f);
        }
        if (!isSongPlayCompleted && currentMusicTimeMSec >= audioSource.clip.length * 1000f - 500f){
            isSongPlayCompleted = true;
        }
        if (isSongPlayCompleted){
            return;
        }

        //表示リスト追加
        for (; ; ){
            // TODO : BPM可変を考慮して出現タイミングを算出する

            if (notes.notes[displayIndex].timeMs - lane.durationMs <= currentMusicTimeMSec){
                NoteObject _noteObject = new NoteObject(notes.notes[displayIndex], notesPool.GetObject());
                noteObjects.Add(_noteObject);
                displayIndex++;
            }else{
                break;
            }
        }

        for (int i = 0; i < noteObjects.Count;i++){
            NoteObject tmpObject = noteObjects[i];
            Note tmpNote = tmpObject.note;

            //座標決定
            float lerpPos = 1 - (tmpNote.timeMs - currentMusicTimeMSec) / lane.durationMs;
            tmpObject.gameObject.transform.position = lane.GetLanePos(tmpNote.laneIndex, lerpPos);

            // TODO : BPM可変を考慮して座標決定する

            if (lerpPos >= 1){
                tmpNote.isJudged = true; // TODO : 臨時で時間超えたら消してるだけ、ほんとは判定後消す
            }

            //表示リストから除外
            if (tmpNote.isJudged){
                noteObjects.RemoveAt(i);
            }
        }

    }
    
}
