using Editor;

public static class MyEditorMenu
{
	[Menu( "Editor", "LichessNET Port/My Menu Option" )]
	public static void OpenMyMenu()
	{
		EditorUtility.DisplayDialog( "It worked!", "This is being called from your library's editor code!" );
	}
}
