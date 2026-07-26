using System;

namespace OpenUtau.Core {
	public class MessageCustomizableException : Exception {

		public override string Message { get; } = string.Empty;
		public string TranslatableMessage { get; set; } = string.Empty;
		public Exception SubstanceException { get; }
		public bool ShowStackTrace { get; } = true;
		public object[]? Replaces { get; }


		public MessageCustomizableException(string message, string translatableMessage, Exception e, bool showStackTrace = true, object[]? replaces = null) {
			if (e is MessageCustomizableException mce) {
				Message = mce.Message;
				TranslatableMessage = mce.TranslatableMessage;
				SubstanceException = mce.SubstanceException;
				ShowStackTrace = mce.ShowStackTrace;
				Replaces = mce.Replaces;
			} else {
				Message = message;
				TranslatableMessage = translatableMessage;
				SubstanceException = e;
				ShowStackTrace = showStackTrace;
				Replaces = replaces;
			}
		}

		public override string ToString() {
			return SubstanceException.Message;
		}
	}
}
