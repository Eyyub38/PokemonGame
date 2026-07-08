using UnityEngine;

public class DialogContext {
    public PlayerController Player { get; private set; }
    public Transform Initiator { get; private set; }
    public GameObject Speaker { get; private set; }
    public Component Source { get; private set; }
    public string SpeakerId { get; private set; }

    public DialogContext(PlayerController player = null, Transform initiator = null, GameObject speaker = null, Component source = null, string speakerId = null) {
        Player = player;
        Initiator = initiator;
        Speaker = speaker;
        Source = source;
        SpeakerId = speakerId;
    }

    public static DialogContext FromInteraction(Component source, Transform initiator, string speakerId = null) {
        var player = initiator != null ? initiator.GetComponent<PlayerController>() : PlayerController.i;
        var speaker = source != null ? source.gameObject : null;
        return new DialogContext(player, initiator, speaker, source, speakerId);
    }

    public T GetSpeakerComponent<T>() where T : Component {
        if(Source != null && Source.TryGetComponent(out T sourceComponent)) {
            return sourceComponent;
        }

        return Speaker != null ? Speaker.GetComponent<T>() : null;
    }

    public T GetPlayerComponent<T>() where T : Component {
        if(Player == null) {
            return null;
        }

        return Player.GetComponent<T>();
    }
}
