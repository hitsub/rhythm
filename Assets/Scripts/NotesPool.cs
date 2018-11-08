using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotesPool : MonoBehaviour {

    // TODO : たぶんリストにして適宜拡張する感じが綺麗
    // TODO : indexが1周すると挙動が怪しいのでデバッグする

    const int NOTES_MAX = 50;

    int index = 0;

    [SerializeField] GameObject note;
    [SerializeField] Transform parent;

    GameObject[] notes = new GameObject[NOTES_MAX];

	void Start () {
        for (int i = 0; i < NOTES_MAX; i++){
            notes[i] = Instantiate(note, new Vector3(0,10,0), Quaternion.identity, parent);
        }
	}
	
    public GameObject GetObject(){
        GameObject res = notes[index];
        index += ((index + 1) == NOTES_MAX) ? -(NOTES_MAX - 1) : 1;
        return res;
    }
}
