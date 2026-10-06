using InMemoryDatabase.Resp.Enums;
using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class SetCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;

        public SetCommandHandler(RespStore store)
        {
            _store = store;
        }
        public string Name => "SET";

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 2)
            {
                return RespValue.Error("ERR wrong number of arguments for 'set' command");
            }

            string key = args[0].TypeString!;
            string value = args[1].TypeString!;

            DateTimeOffset? expiresAt = null;
            SetCondition condition = SetCondition.Always;

            // 2 = skipping key value
            for (int i = 2; i < args.Length; i++)
            {
                string option = args[i].TypeString!;

                // check if it containd additional set argument
                if (option.Equals("EX", StringComparison.OrdinalIgnoreCase) || option.Equals("PX", StringComparison.OrdinalIgnoreCase))
                {
                    if (expiresAt is not null || i + 1 >= args.Length)
                    {
                        return RespValue.Error("ERR syntax error");
                    }

                    // consumes ++i number which follows EX/PX, so it does not read it as option
                    if (!long.TryParse(args[++i].TypeString, out var amount))
                    {
                        return RespValue.Error("ERR value is not an integer or out of range");
                    }

                    if (amount <= 0)
                    {
                        return RespValue.Error("ERR invalid expire time in 'set' command");
                    }

                    // EX seconds | PX milliseconds
                    bool isSeconds = option.Equals("EX", StringComparison.OrdinalIgnoreCase);

                    try
                    {
                        // 2 possible values in amount, seconds or milliseconds
                        expiresAt = isSeconds
                            ? DateTimeOffset.UtcNow.AddSeconds(amount)
                            : DateTimeOffset.UtcNow.AddMilliseconds(amount);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // past DateTimeOffset.MaxValue
                        return RespValue.Error("ERR invalid expire time in 'set' command");
                    }
                }
                else if (option.Equals("NX", StringComparison.OrdinalIgnoreCase))
                {
                    // condition of set command does not match argument
                    if (condition != SetCondition.Always)
                    {
                        return RespValue.Error("ERR syntax error");
                    }

                    condition = SetCondition.IfNotExists;
                }
                else if (option.Equals("XX", StringComparison.OrdinalIgnoreCase))
                {
                    // condition of set command does not match argument
                    if (condition != SetCondition.Always)
                    {
                        return RespValue.Error("ERR syntax error");
                    }

                    condition = SetCondition.IfExists;
                }
                else
                {
                    return RespValue.Error("ERR syntax error");
                }
            }
            bool written = _store.Set(key, RespValue.BulkString(value), expiresAt, condition);

            return written ? RespValue.SimpleString("OK") : RespValue.Null;
        }
    }
}
