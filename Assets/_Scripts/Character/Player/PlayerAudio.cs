
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PlayerAudio : MonoBehaviour
{
    List<AudioSource> audios = new();

    private Player player = null;

    private void Start()
    {
        player = GetComponent<Player>();
        audios = gameObject.GetComponents<AudioSource>().ToList();

        player.Status.OnDamaged += OnDamaged;
        //player.Physics.OnCollectableTrigger += OnCollectableTrigger;
    }

    private void OnDamaged(int health)
    {
        if (health == 0) { audios[0].mute = false; audios[0].Play(); }
        else { audios[1].mute = false; audios[1].Play(); }
    }

    private void OnCollectableTrigger(Collider2D collider)
    {
        if (player.Status.Dead) { return; }

        string cName = collider.gameObject.name;

        if (cName.Contains("Jetpack") || cName.Contains("Shield") || cName.Contains("JumpShoes")) { audios[2].mute = false; audios[2].Play(); }
        else if (cName.Contains("Coin")) { audios[3].mute = false; audios[3].Play(); }
    }
}
