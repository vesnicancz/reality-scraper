namespace RealityScraper.Application.Interfaces.Mailing;

/// <summary>
/// Příloha e-mailu nezávislá na konkrétním poskytovateli odesílání.
/// </summary>
/// <param name="FileName">Název souboru přílohy.</param>
/// <param name="Content">Obsah přílohy.</param>
/// <param name="ContentType">MIME typ přílohy.</param>
/// <param name="ContentId">Identifikátor pro inline vložení do HTML přes cid: schéma; null pro běžnou přílohu.</param>
public record EmailAttachmentData(string FileName, byte[] Content, string ContentType, string? ContentId);
