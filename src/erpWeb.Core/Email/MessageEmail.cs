namespace erpWeb.Core.Email;

public sealed record MessageEmail(string Destinataire, string Sujet, string CorpsHtml);
