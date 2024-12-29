﻿using Sidekick.Common.Game.Items;
using Sidekick.Common.Initialization;

namespace Sidekick.Apis.Poe.Parser.Headers;

public interface IHeaderParser : IInitializableService
{
    Header Parse(ParsingItem parsingItem);
}
