using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic word-search generator for every level past the hand-made ones in the scene.
/// The same level number always produces the same title, grid and words.
/// Change TotalLevels to change how many levels the game has.
/// </summary>
public static class ProceduralLevels
{
    public const int TotalLevels = 500;

    public struct Data
    {
        public string title;
        public string[] rows;
        public List<string> words;
        public List<Vector2Int[]> cells; // (row, col) per letter
    }

    // ------------------------------------------------------------------ word bank
    private static readonly string[][] Themes =
    {
        new[] { "ANIMALS", "LION", "TIGER", "ZEBRA", "GIRAFFE", "MONKEY", "RABBIT", "ELEPHANT", "KANGAROO", "PANDA", "CAMEL", "WOLF", "FOX", "HIPPO", "OTTER", "BADGER", "LEOPARD", "MOOSE" },
        new[] { "FRUITS", "APPLE", "BANANA", "CHERRY", "GRAPE", "LEMON", "MANGO", "ORANGE", "PEACH", "PEAR", "PLUM", "KIWI", "MELON", "PAPAYA", "GUAVA", "APRICOT", "COCONUT", "LIME" },
        new[] { "VEGETABLES", "CARROT", "POTATO", "TOMATO", "ONION", "GARLIC", "CABBAGE", "SPINACH", "PEPPER", "LETTUCE", "PEAS", "BEANS", "CELERY", "RADISH", "PUMPKIN", "TURNIP", "LEEK", "BROCCOLI" },
        new[] { "COLORS", "RED", "BLUE", "GREEN", "YELLOW", "PURPLE", "ORANGE", "PINK", "BROWN", "BLACK", "WHITE", "GREY", "VIOLET", "INDIGO", "MAROON", "TEAL", "CRIMSON", "GOLDEN" },
        new[] { "BODY", "HEAD", "HAND", "FOOT", "KNEE", "ELBOW", "SHOULDER", "FINGER", "ANKLE", "WRIST", "NOSE", "MOUTH", "CHIN", "EYEBROW", "THUMB", "HEART", "LUNGS", "SPINE" },
        new[] { "CLOTHES", "SHIRT", "JACKET", "SCARF", "GLOVES", "BOOTS", "SOCKS", "DRESS", "SKIRT", "JEANS", "SWEATER", "HAT", "COAT", "SANDALS", "BELT", "HOODIE", "SHORTS", "VEST" },
        new[] { "VEHICLES", "CAR", "TRUCK", "BUS", "TRAIN", "PLANE", "BOAT", "BICYCLE", "SCOOTER", "TAXI", "TRACTOR", "SUBWAY", "TRAM", "HELICOPTER", "YACHT", "VAN", "JEEP", "ROCKET" },
        new[] { "CITY", "STREET", "BRIDGE", "TOWER", "PARK", "MARKET", "SUBWAY", "TRAFFIC", "OFFICE", "MALL", "PLAZA", "AVENUE", "HARBOR", "STADIUM", "CINEMA", "LIBRARY", "HOTEL", "MUSEUM" },
        new[] { "BEACH", "SAND", "WAVES", "SHELL", "TOWEL", "SUNHAT", "SURF", "CRAB", "PALM", "TIDE", "BUCKET", "SPADE", "DUNES", "PIER", "LIFEGUARD", "SUNSET", "OCEAN", "SEAGULL" },
        new[] { "WINTER", "SNOW", "FROST", "SLED", "SKATES", "MITTENS", "SCARF", "ICICLE", "BLIZZARD", "COCOA", "IGLOO", "FIREPLACE", "CHILL", "SNOWMAN", "PENGUIN", "FLAKE", "COLD", "ICE" },
        new[] { "SUMMER", "SUNNY", "HOLIDAY", "PICNIC", "SWIM", "LEMONADE", "ICECREAM", "BREEZE", "CAMPING", "SANDALS", "SHADES", "POOL", "HEAT", "FAN", "BARBECUE", "FESTIVAL", "BEACH", "TAN" },
        new[] { "AUTUMN", "LEAVES", "HARVEST", "PUMPKIN", "ACORN", "MAPLE", "RAKE", "CIDER", "SCARECROW", "SQUASH", "CHESTNUT", "BREEZE", "FOGGY", "AMBER", "ORCHARD", "COZY", "FALL", "OWL" },
        new[] { "SPRING", "BLOSSOM", "TULIP", "RAIN", "PUDDLE", "BUD", "NEST", "CHICK", "SPROUT", "DAISY", "BREEZE", "MEADOW", "LAMB", "SEEDS", "BLOOM", "PETAL", "GREEN", "BEES" },
        new[] { "BAKERY", "BREAD", "FLOUR", "YEAST", "DOUGH", "OVEN", "CROISSANT", "BAGEL", "MUFFIN", "PRETZEL", "BUTTER", "SUGAR", "WHISK", "ROLLS", "CRUST", "BAGUETTE", "SCONE", "PIE" },
        new[] { "BREAKFAST", "EGGS", "BACON", "TOAST", "CEREAL", "PANCAKE", "WAFFLE", "COFFEE", "JUICE", "YOGURT", "OATMEAL", "HONEY", "JAM", "MILK", "SAUSAGE", "OMELETTE", "BERRIES", "TEA" },
        new[] { "DRINKS", "WATER", "JUICE", "COFFEE", "TEA", "SODA", "MILK", "SMOOTHIE", "LEMONADE", "COCOA", "SHAKE", "LATTE", "MOCHA", "PUNCH", "TONIC", "NECTAR", "CIDER", "BROTH" },
        new[] { "SPICES", "PEPPER", "SALT", "CUMIN", "GINGER", "GARLIC", "CINNAMON", "NUTMEG", "PAPRIKA", "SAFFRON", "CLOVE", "MINT", "BASIL", "OREGANO", "THYME", "CHILI", "TURMERIC", "FENNEL" },
        new[] { "OCEAN LIFE", "WHALE", "SHARK", "DOLPHIN", "OCTOPUS", "SQUID", "SEAL", "TURTLE", "JELLYFISH", "STARFISH", "LOBSTER", "SHRIMP", "CORAL", "EEL", "MANTA", "CLAM", "OYSTER", "WALRUS" },
        new[] { "REPTILES", "SNAKE", "LIZARD", "GECKO", "IGUANA", "TURTLE", "TORTOISE", "COBRA", "PYTHON", "VIPER", "ALLIGATOR", "CROCODILE", "CHAMELEON", "SKINK", "ADDER", "BOA", "DRAGON", "MAMBA" },
        new[] { "DINOSAURS", "TREX", "RAPTOR", "FOSSIL", "BONES", "JURASSIC", "STEGOSAUR", "EXTINCT", "COMET", "SKULL", "CLAWS", "TAIL", "EGG", "SWAMP", "FERNS", "VOLCANO", "HORNS", "SCALES" },
        new[] { "PETS", "DOG", "CAT", "HAMSTER", "PARROT", "GOLDFISH", "RABBIT", "TURTLE", "PUPPY", "KITTEN", "LEASH", "COLLAR", "BOWL", "TREAT", "CAGE", "FERRET", "PONY", "GERBIL" },
        new[] { "TREES", "OAK", "PINE", "MAPLE", "BIRCH", "WILLOW", "CEDAR", "PALM", "ASH", "ELM", "SPRUCE", "REDWOOD", "BAMBOO", "CYPRESS", "POPLAR", "BARK", "ROOTS", "BRANCH" },
        new[] { "NATURE", "RIVER", "LAKE", "MEADOW", "FOREST", "VALLEY", "HILL", "STREAM", "CANYON", "DESERT", "JUNGLE", "ISLAND", "LAGOON", "CAVE", "WATERFALL", "MARSH", "PRAIRIE", "GLADE" },
        new[] { "GEMS", "DIAMOND", "RUBY", "EMERALD", "SAPPHIRE", "PEARL", "OPAL", "TOPAZ", "AMBER", "JADE", "GARNET", "QUARTZ", "ONYX", "CRYSTAL", "AMETHYST", "GOLD", "SILVER", "JEWEL" },
        new[] { "TOOLS", "HAMMER", "WRENCH", "SAW", "DRILL", "PLIERS", "SCREW", "NAIL", "LADDER", "CHISEL", "SHOVEL", "TAPE", "LEVEL", "CLAMP", "FILE", "AXE", "BOLT", "SANDER" },
        new[] { "OFFICE", "DESK", "CHAIR", "LAPTOP", "STAPLER", "PAPER", "FOLDER", "PRINTER", "MEETING", "EMAIL", "PHONE", "CALENDAR", "MEMO", "BINDER", "MARKER", "BOSS", "REPORT", "BADGE" },
        new[] { "COMPUTER", "MOUSE", "SCREEN", "KEYBOARD", "SPEAKER", "LAPTOP", "MEMORY", "CODE", "PIXEL", "CURSOR", "BROWSER", "DATA", "CHIP", "ROUTER", "CABLE", "SERVER", "LOGIN", "FILE" },
        new[] { "INSTRUMENTS", "VIOLIN", "CELLO", "PIANO", "GUITAR", "DRUMS", "FLUTE", "TRUMPET", "HARP", "BANJO", "TUBA", "OBOE", "CLARINET", "ORGAN", "UKULELE", "SITAR", "BUGLE", "LUTE" },
        new[] { "MOVIES", "ACTOR", "CAMERA", "SCENE", "SCRIPT", "DIRECTOR", "POPCORN", "TICKET", "SCREEN", "COMEDY", "DRAMA", "HERO", "VILLAIN", "SEQUEL", "TRAILER", "STUDIO", "PREMIERE", "CAST" },
        new[] { "GAMES", "PUZZLE", "CHESS", "CARDS", "DICE", "BOARD", "LEVEL", "SCORE", "PLAYER", "WINNER", "TOKEN", "MAZE", "QUIZ", "RIDDLE", "BINGO", "DOMINO", "TROPHY", "BONUS" },
        new[] { "TOYS", "DOLL", "BLOCKS", "KITE", "YOYO", "ROBOT", "TEDDY", "BALL", "TRAIN", "PUPPET", "MARBLES", "LEGO", "DRUM", "SLINKY", "RATTLE", "TOP", "WAGON", "SPINNER" },
        new[] { "CASTLE", "KING", "QUEEN", "KNIGHT", "TOWER", "MOAT", "THRONE", "CROWN", "DRAGON", "SWORD", "SHIELD", "PRINCE", "DUNGEON", "BANNER", "ARMOR", "GATE", "JESTER", "WIZARD" },
        new[] { "PIRATES", "SHIP", "PARROT", "TREASURE", "MAP", "ANCHOR", "CAPTAIN", "CANNON", "ISLAND", "SAIL", "PLANK", "COMPASS", "CHEST", "GOLD", "HOOK", "DECK", "FLAG", "CREW" },
        new[] { "FANTASY", "MAGIC", "WIZARD", "FAIRY", "ELF", "DRAGON", "UNICORN", "POTION", "SPELL", "WAND", "GOBLIN", "TROLL", "CASTLE", "QUEST", "GIANT", "PHOENIX", "ORACLE", "RUNE" },
        new[] { "SCIENCE", "ATOM", "CELL", "ENERGY", "GRAVITY", "MAGNET", "LASER", "PLASMA", "OXYGEN", "CARBON", "PROTON", "LENS", "FORMULA", "THEORY", "LAB", "ELEMENT", "FOSSIL", "GENE" },
        new[] { "MATH", "NUMBER", "ADD", "MINUS", "DIVIDE", "ANGLE", "CIRCLE", "SQUARE", "TRIANGLE", "FRACTION", "DECIMAL", "EQUAL", "GRAPH", "RATIO", "SUM", "ZERO", "PRIME", "CUBE" },
        new[] { "SHAPES", "CIRCLE", "SQUARE", "OVAL", "STAR", "HEART", "DIAMOND", "CUBE", "SPHERE", "CONE", "PYRAMID", "HEXAGON", "OCTAGON", "ARROW", "CROSS", "RING", "PRISM", "ARC" },
        new[] { "FAMILY", "MOTHER", "FATHER", "SISTER", "BROTHER", "UNCLE", "AUNT", "COUSIN", "NIECE", "NEPHEW", "BABY", "TWINS", "GRANDMA", "GRANDPA", "SON", "DAUGHTER", "PARENT", "HOME" },
        new[] { "FEELINGS", "HAPPY", "SAD", "ANGRY", "CALM", "BRAVE", "PROUD", "SHY", "EXCITED", "NERVOUS", "JOY", "LOVE", "HOPE", "CURIOUS", "BORED", "GRATEFUL", "SURPRISED", "CONTENT" },
        new[] { "HOBBIES", "READING", "PAINTING", "COOKING", "DANCING", "SINGING", "FISHING", "HIKING", "KNITTING", "CYCLING", "GARDEN", "CHESS", "POTTERY", "SKETCH", "BAKING", "YOGA", "PUZZLES", "DIARY" },
        new[] { "OLYMPICS", "MEDAL", "TORCH", "SPRINT", "JAVELIN", "DISCUS", "RELAY", "HURDLES", "ROWING", "FENCING", "ARCHERY", "DIVING", "PODIUM", "RECORD", "ATHLETE", "MARATHON", "VAULT", "JUDO" },
        new[] { "WEATHER", "THUNDER", "STORM", "CLOUD", "RAINBOW", "HAIL", "SLEET", "TORNADO", "BREEZE", "HUMID", "FOGGY", "DRIZZLE", "SUNNY", "MONSOON", "WINDY", "SHOWER", "FROST", "MIST" },
        new[] { "PLANETS", "MERCURY", "VENUS", "EARTH", "MARS", "JUPITER", "SATURN", "URANUS", "NEPTUNE", "PLUTO", "MOON", "SUN", "STAR", "ORBIT", "RINGS", "CRATER", "COMET", "SPACE" },
        new[] { "RESTAURANT", "MENU", "WAITER", "CHEF", "TABLE", "ORDER", "DESSERT", "BILL", "TIP", "NAPKIN", "SOUP", "SALAD", "STEAK", "PASTA", "PLATE", "GLASS", "KITCHEN", "BOOTH" },
        new[] { "HOSPITAL", "DOCTOR", "NURSE", "PATIENT", "BANDAGE", "XRAY", "CLINIC", "MEDICINE", "PILLS", "STRETCHER", "SURGEON", "WARD", "HEALTH", "SYRINGE", "SCALPEL", "MASK", "CAST", "TEMPERATURE" },
        new[] { "AIRPORT", "PILOT", "RUNWAY", "GATE", "LUGGAGE", "TICKET", "TERMINAL", "BOARDING", "PASSPORT", "JET", "CABIN", "HANGAR", "TOWER", "CUSTOMS", "LANDING", "SECURITY", "FLIGHT", "WING" },
        new[] { "FARM LIFE", "BARN", "TRACTOR", "HAY", "CHICKEN", "ROOSTER", "GOAT", "SHEEP", "CORN", "WHEAT", "FIELD", "FENCE", "PLOW", "DUCK", "SILO", "STABLE", "MILK", "EGGS" },
        new[] { "CIRCUS", "CLOWN", "TENT", "JUGGLER", "ACROBAT", "TRAPEZE", "LION", "RINGMASTER", "MAGIC", "BALLOON", "POPCORN", "UNICYCLE", "STILTS", "TICKET", "ELEPHANT", "CANNON", "SHOW", "CROWD" },
        new[] { "PARTY", "CAKE", "BALLOON", "CANDLE", "GIFTS", "MUSIC", "DANCE", "GAMES", "CONFETTI", "SNACKS", "FRIENDS", "HATS", "INVITE", "BANNER", "CHEER", "PUNCH", "SURPRISE", "FUN" },
        new[] { "INSECTS", "BEETLE", "BUTTERFLY", "LADYBUG", "MOSQUITO", "CRICKET", "HORNET", "TERMITE", "FIREFLY", "LOCUST", "APHID", "MANTIS", "DRAGONFLY", "BEE", "FLEA", "WASP", "MOTH", "ANT" },
        new[] { "CANDY", "LOLLIPOP", "TOFFEE", "FUDGE", "GUMMY", "CARAMEL", "MINT", "TAFFY", "NOUGAT", "LICORICE", "TRUFFLE", "SPRINKLES", "JELLY", "COTTON", "CHOCOLATE", "CANDYCANE", "BONBON", "SUGAR" },
        new[] { "RAINFOREST", "JAGUAR", "TOUCAN", "SLOTH", "VINES", "CANOPY", "PARROT", "MONKEY", "ORCHID", "FROG", "MOSS", "RIVER", "TAPIR", "GORILLA", "LIANA", "HUMID", "FERN", "ANACONDA" },
        new[] { "ARCTIC", "POLAR", "SEAL", "WALRUS", "ICEBERG", "GLACIER", "TUNDRA", "HUSKY", "SLED", "AURORA", "IGLOO", "FROZEN", "PENGUIN", "NARWHAL", "CARIBOU", "SNOWY", "FJORD", "OWL" },
        new[] { "DESERT", "CACTUS", "CAMEL", "DUNE", "OASIS", "SAND", "MIRAGE", "SCORPION", "LIZARD", "HEAT", "SUNSET", "CANYON", "MESA", "DRY", "NOMAD", "VULTURE", "SAHARA", "PALM" },
        new[] { "ROBOTS", "GEARS", "CIRCUIT", "SENSOR", "BATTERY", "LASER", "METAL", "ANTENNA", "MOTOR", "WIRES", "ANDROID", "DRONE", "CYBORG", "BOLTS", "SIGNAL", "POWER", "BEEP", "CHIP" },
        new[] { "ART", "PAINT", "BRUSH", "CANVAS", "EASEL", "SKETCH", "PALETTE", "STATUE", "GALLERY", "PORTRAIT", "MURAL", "COLOR", "CRAYON", "CHARCOAL", "INK", "CLAY", "FRAME", "STENCIL" },
        new[] { "WRITING", "PEN", "PENCIL", "PAPER", "STORY", "POEM", "AUTHOR", "CHAPTER", "NOVEL", "LETTER", "JOURNAL", "WORDS", "ESSAY", "DRAFT", "TITLE", "PLOT", "VERSE", "NOTE" },
        new[] { "KITCHEN TOOLS", "PAN", "POT", "LADLE", "SPATULA", "WHISK", "GRATER", "KETTLE", "TOASTER", "BLENDER", "MIXER", "TONGS", "PEELER", "STRAINER", "ROLLER", "OVEN", "KNIFE", "MUG" },
        new[] { "GARDENING", "SOIL", "SEEDS", "RAKE", "HOE", "TROWEL", "SPROUT", "WATERING", "COMPOST", "WEEDS", "HEDGE", "SHEARS", "BULB", "MULCH", "ROSES", "VINE", "SHED", "POT" },
        new[] { "SEA TRAVEL", "FERRY", "CRUISE", "ANCHOR", "HARBOR", "DOCK", "SAILOR", "CAPTAIN", "VOYAGE", "PORT", "BUOY", "LIGHTHOUSE", "MAST", "HULL", "DECK", "WAVES", "ROPE", "OAR" },
    };

    public static int ThemeCount => Themes.Length;

    private static int ThemeIndex(int levelIndex) => ((levelIndex * 7) + 3) % Themes.Length; // 7 is coprime with the theme count, so neighbours differ

    public static string TitleFor(int levelIndex) => Themes[ThemeIndex(levelIndex)][0];

    // ------------------------------------------------------------------ generation
    private static readonly Vector2Int[] Forward = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(-1, 1) };
    private static readonly Vector2Int[] Backward = { new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(1, -1) };

    /// <param name="levelIndex">0-based level index.</param>
    /// <param name="size">Grid size (rows = columns).</param>
    /// <param name="firstProceduralIndex">Index of the first generated level (used for the difficulty ramp).</param>
    public static Data Generate(int levelIndex, int size, int firstProceduralIndex)
    {
        string[] theme = Themes[ThemeIndex(levelIndex)];
        Rng rng = new Rng((uint)(levelIndex * 2654435761u) ^ 0x9E3779B9u);

        float t = Mathf.Clamp01((levelIndex - firstProceduralIndex) / (float)Mathf.Max(1, TotalLevels - 1 - firstProceduralIndex));
        int wantWords = Mathf.Clamp(7 + Mathf.RoundToInt(t * 5f), 5, 12);           // 7 -> 12 words
        float backwardChance = t < 0.1f ? 0.15f : Mathf.Lerp(0.3f, 0.6f, t);         // more reversed words later on

        // candidate words that fit the grid, shuffled
        List<string> pool = new List<string>();
        for (int i = 1; i < theme.Length; i++)
        {
            string w = theme[i].Replace(" ", "").ToUpperInvariant();
            if (w.Length >= 3 && w.Length <= size && !pool.Contains(w)) pool.Add(w);
        }
        for (int i = pool.Count - 1; i > 0; i--) { int j = rng.Range(0, i + 1); string tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp; }

        char[,] grid = new char[size, size];
        List<string> placed = new List<string>();
        List<Vector2Int[]> placedCells = new List<Vector2Int[]>();

        // pass 1: a random selection, longest first (fits much better); pass 2: top up from the rest of the theme
        int firstPick = Mathf.Min(pool.Count, wantWords);
        List<string> chosen = pool.GetRange(0, firstPick);
        chosen.Sort((x, y) => y.Length.CompareTo(x.Length));
        List<string> rest = pool.GetRange(firstPick, pool.Count - firstPick);
        rest.Sort((x, y) => y.Length.CompareTo(x.Length));
        chosen.AddRange(rest);

        foreach (string word in chosen)
        {
            if (placed.Count >= wantWords) break;
            if (ContainsOverlap(placed, word)) continue; // avoid e.g. "CAT" + "CATS"
            Vector2Int[] cells = TryPlace(grid, word, size, rng, backwardChance);
            if (cells == null) continue;
            placed.Add(word);
            placedCells.Add(cells);
        }

        // filler letters: mostly letters taken from this level's words so the board is harder to scan
        string letters = string.Concat(placed) + "ABCDEFGHIJKLMNOPRSTUVWY";
        string[] rows = new string[size];
        for (int r = 0; r < size; r++)
        {
            char[] row = new char[size];
            for (int c = 0; c < size; c++)
            {
                if (grid[r, c] == '\0') grid[r, c] = letters[rng.Range(0, letters.Length)];
                row[c] = grid[r, c];
            }
            rows[r] = new string(row);
        }

        // show the chips in a stable, readable order (as placed: long to short)
        return new Data { title = theme[0], rows = rows, words = placed, cells = placedCells };
    }

    private static bool ContainsOverlap(List<string> placed, string word)
    {
        foreach (string p in placed)
            if (p.Contains(word) || word.Contains(p)) return true;
        return false;
    }

    private static Vector2Int[] TryPlace(char[,] grid, string word, int size, Rng rng, float backwardChance)
    {
        for (int attempt = 0; attempt < 250; attempt++)
        {
            Vector2Int[] set = rng.Next01() < backwardChance ? Backward : Forward;
            Vector2Int d = set[rng.Range(0, set.Length)];
            int r0 = rng.Range(0, size), c0 = rng.Range(0, size);
            int r1 = r0 + d.x * (word.Length - 1), c1 = c0 + d.y * (word.Length - 1);
            if (r1 < 0 || r1 >= size || c1 < 0 || c1 >= size) continue;

            bool ok = true;
            for (int i = 0; i < word.Length && ok; i++)
            {
                char g = grid[r0 + d.x * i, c0 + d.y * i];
                if (g != '\0' && g != word[i]) ok = false;
            }
            if (!ok) continue;

            Vector2Int[] cells = new Vector2Int[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                int r = r0 + d.x * i, c = c0 + d.y * i;
                grid[r, c] = word[i];
                cells[i] = new Vector2Int(r, c);
            }
            return cells;
        }

        // random tries failed: scan every start cell / direction in a shuffled order
        List<int> starts = new List<int>();
        for (int i = 0; i < size * size * 8; i++) starts.Add(i);
        for (int i = starts.Count - 1; i > 0; i--) { int j = rng.Range(0, i + 1); int tmp = starts[i]; starts[i] = starts[j]; starts[j] = tmp; }
        foreach (int code in starts)
        {
            int cell = code / 8, dir = code % 8;
            Vector2Int d = dir < 4 ? Forward[dir] : Backward[dir - 4];
            int r0 = cell / size, c0 = cell % size;
            int r1 = r0 + d.x * (word.Length - 1), c1 = c0 + d.y * (word.Length - 1);
            if (r1 < 0 || r1 >= size || c1 < 0 || c1 >= size) continue;
            bool ok = true;
            for (int i = 0; i < word.Length && ok; i++)
            {
                char g = grid[r0 + d.x * i, c0 + d.y * i];
                if (g != '\0' && g != word[i]) ok = false;
            }
            if (!ok) continue;
            Vector2Int[] cells = new Vector2Int[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                grid[r0 + d.x * i, c0 + d.y * i] = word[i];
                cells[i] = new Vector2Int(r0 + d.x * i, c0 + d.y * i);
            }
            return cells;
        }
        return null;
    }

    /// <summary>Tiny xorshift RNG so levels are identical on every device / Unity version.</summary>
    private class Rng
    {
        private uint s;
        public Rng(uint seed) { s = seed == 0 ? 0x6D2B79F5u : seed; for (int i = 0; i < 4; i++) NextU(); }
        private uint NextU() { s ^= s << 13; s ^= s >> 17; s ^= s << 5; return s; }
        public int Range(int min, int maxExclusive) => min + (int)(NextU() % (uint)Mathf.Max(1, maxExclusive - min));
        public float Next01() => (NextU() & 0xFFFFFF) / 16777216f;
    }
}
