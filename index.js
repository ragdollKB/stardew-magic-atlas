const coloredSpots = {};
const mapHeadings = {};

function drawBackgroundImage(backgroundCanvas, src) {
    return new Promise(resolve => {
        const image = new Image();
        image.addEventListener('load', () => {
            const { width, height } = image;
            backgroundCanvas.width = width;
            backgroundCanvas.height = height;
            const stageDiv = backgroundCanvas.parentElement;
            stageDiv.style = `height: ${height}px; width: ${width}px;`;

            const ctx = backgroundCanvas.getContext('2d');
            ctx.drawImage(image, 0, 0);
            ctx.strokeStyle = '#fffa';
            for (let x = 0; x <= width; x += 16) {
                ctx.beginPath();
                ctx.moveTo(x, 0);
                ctx.lineTo(x, height);
                ctx.stroke();
            }
            for (let y = 0; y <= height; y += 16) {
                ctx.beginPath();
                ctx.moveTo(0, y);
                ctx.lineTo(width, y);
                ctx.stroke();
            }

            resolve([width, height]);
        });
        
        image.addEventListener('error', (err) => {
            console.error(`Failed to load image: ${src}`, err);
            resolve([0, 0]); // Resolve with zero dimensions to avoid breaking the chain
        });

        image.src = src;
    });
}

function initMap(title, src) {
    // Create a unique ID for the map section
    const mapId = `map-${title.replace(/[^a-zA-Z0-9]/g, '-').toLowerCase()}`;
    
    // Create a map section with the ID
    const mapSection = document.createElement('section');
    mapSection.id = mapId;
    document.body.appendChild(mapSection);

    const titleHeading = document.createElement('h2');
    titleHeading.innerText = title;
    titleHeading.id = `heading-${mapId}`;
    mapSection.appendChild(titleHeading);
    
    // Store the heading reference for navigation
    mapHeadings[title] = titleHeading;

    const stageDiv = document.createElement('div');
    stageDiv.className = 'canvas-stage';
    mapSection.appendChild(stageDiv);

    const backgroundCanvas = document.createElement('canvas');
    stageDiv.appendChild(backgroundCanvas);

    return drawBackgroundImage(backgroundCanvas, src).then(([width, height]) => {
        if (width === 0 && height === 0) {
            // Image failed to load
            titleHeading.innerHTML += ' <span style="color: #ff5555">(Failed to load)</span>';
            return false;
        }
        
        const frontCanvas = document.createElement('canvas');
        frontCanvas.width = width;
        frontCanvas.height = height;
        frontCanvas.id = `${title}-front-canvas`;
        stageDiv.appendChild(frontCanvas);

        const ctx = frontCanvas.getContext('2d');
        let highlightedCoord = null;
        frontCanvas.addEventListener('mousemove', ({offsetX, offsetY}) => {
            const x = Math.floor(offsetX / 16);
            const y = Math.floor(offsetY / 16);
            const coloredSpot = coloredSpots[title]?.[x]?.[y];
            const coloredSpotText = coloredSpot ? `${coloredSpot.join(', ')} ` : '';
            tooltip.innerText = `${title}: ${coloredSpotText}(${x},${y})`;

            if (highlightedCoord) {
                const [oldX, oldY] = highlightedCoord;
                if (x === oldX && y === oldY) {
                    // User didn't move out of the square, so keep old highlight.
                    return;
                }

                ctx.clearRect(oldX * 16 + 1, oldY * 16 + 1, 14, 14);

                // This spot was already occupied, so re-color it.
                if (coloredSpots[title]?.[oldX]?.[oldY]) {
                    ctx.fillStyle = '#ff0a';
                    ctx.fillRect(oldX * 16 + 1, oldY * 16 + 1, 14, 14);
                }
            }

            ctx.fillStyle = '#fffa';
            ctx.fillRect(x * 16 + 1, y * 16 + 1, 14, 14);
            highlightedCoord = [x, y];
        });
        
        return true;
    });
}

function colorSpots(spots) {
    const spotKeys = Object.keys(spots);
    for (const spotKey of spotKeys) {
        const spot = spots[spotKey];

        const [mapName, xStr, yStr, , modifier] = spot.split(' ');
        const x = Number(xStr);
        const y = Number(yStr);
        const frontCanvas = document.querySelector(`#${mapName}-front-canvas`);
        if (!frontCanvas) {
            console.error('bad map name in spots.json:', mapName);
            return;
        }

        let squareX = 1;
        let squareY = 1;
        if (modifier?.startsWith('square_')) {
            [ , squareX, squareY] = modifier.split('_');
            squareX = Number(squareX);
            squareY = Number(squareY);
        }
        const initialX = x - Math.floor(squareX / 2);
        const initialY = y - Math.floor(squareY / 2);

        for (let offsetX = 0; offsetX < squareX; offsetX++) {
            for (let offsetY = 0; offsetY < squareY; offsetY++) {
                const finalX = initialX + offsetX;
                const finalY = initialY + offsetY;
                if (!coloredSpots[mapName]) {
                    coloredSpots[mapName] = {};
                }
                if (!coloredSpots[mapName][finalX]) {
                    coloredSpots[mapName][finalX] = {};
                }
                if (!coloredSpots[mapName][finalX][finalY]) {
                    coloredSpots[mapName][finalX][finalY] = [];
                }
                const spotAlreadyColored = coloredSpots[mapName][finalX][finalY].length;
                coloredSpots[mapName][finalX][finalY].push(spotKey);

                if (!spotAlreadyColored) {
                    const ctx = frontCanvas.getContext('2d');
                    ctx.fillStyle = '#ff0a';
                    ctx.fillRect(finalX * 16 + 1, finalY * 16 + 1, 14, 14);
                }
            }
        }
    }
}

// Function to dynamically fetch map files from the assets directory
async function fetchMapList() {
    try {
        // Create a list of maps using your assets/maps directory
        const mapFiles = [
            'AbandonedJojaMart.png',
            'AdventureGuild.png',
            'AnimalShop.png',
            'ArchaeologyHouse.png',
            'Backwoods.png',
            'BathHouse_Entry.png',
            'BathHouse_MensLocker.png',
            'BathHouse_Pool.png',
            'BathHouse_WomensLocker.png',
            'Beach.png',
            'Blacksmith.png',
            'BoatTunnel.png',
            'BugLand.png',
            'BusStop.png',
            'Caldera.png',
            'cave.png',
            'Cellar.png',
            'Club.png',
            'CommunityCenter_Joja.png',
            'CommunityCenter_Refurbished.png',
            'CommunityCenter_Ruins.png',
            'Desert.png',
            'ElliottHouse.png',
            'Farm.png',
            'FarmCave.png',
            'FishShop.png',
            'Forest.png',
            'Greenhouse.png',
            'HaleyHouse.png',
            'HarveyRoom.png',
            'Hospital.png',
            'Island_CaptainRoom.png',
            'Island_E.png',
            'Island_FarmCave.png',
            'Island_Hut.png',
            'Island_N.png',
            'Island_Resort.png',
            'Island_S.png',
            'Island_SE.png',
            'Island_Secret.png',
            'Island_Shrine.png',
            'Island_W.png',
            'IslandFarmHouse.png',
            'JojaMart.png',
            'JoshHouse.png',
            'LeahHouse.png',
            'LeoTreeHouse.png',
            'LewisBasement.png',
            'ManorHouse.png',
            'MarnieBarn.png',
            'MaruBasement.png',
            'MasteryCave.png',
            'Mine.png',
            'Mountain.png',
            'MovieTheater.png',
            'Railroad.png',
            'Town.png',
            'WizardHouse.png',
            'WizardHouseBasement.png',
            'Woods.png'
        ];

        return mapFiles;
    } catch (error) {
        console.error("Error fetching map list:", error);
        return [];
    }
}

// Create navigation links for all maps
function createNavigation(mapTitles) {
    const mapLinks = document.getElementById('map-links');
    
    // Sort map titles alphabetically
    mapTitles.sort();
    
    // Create a list for the links
    const navList = document.createElement('ul');
    mapLinks.appendChild(navList);
    
    // Add links for each map
    for (const title of mapTitles) {
        const mapId = `map-${title.replace(/[^a-zA-Z0-9]/g, '-').toLowerCase()}`;
        const listItem = document.createElement('li');
        const link = document.createElement('a');
        link.href = `#heading-${mapId}`;
        link.textContent = title;
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const heading = document.getElementById(`heading-${mapId}`);
            if (heading) {
                heading.scrollIntoView({ behavior: 'smooth' });
            }
        });
        listItem.appendChild(link);
        navList.appendChild(listItem);
    }
}

// Set up drag and drop functionality
function setupDragAndDrop() {
    const dropzone = document.getElementById('dropzone');
    
    // Prevent default behaviors for drag events
    ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
        dropzone.addEventListener(eventName, preventDefaults, false);
        document.body.addEventListener(eventName, preventDefaults, false);
    });
    
    // Highlight the dropzone when a file is dragged over it
    ['dragenter', 'dragover'].forEach(eventName => {
        dropzone.addEventListener(eventName, highlight, false);
    });
    
    // Remove highlight when the file is dragged out or dropped
    ['dragleave', 'drop'].forEach(eventName => {
        dropzone.addEventListener(eventName, unhighlight, false);
    });
    
    // Handle the dropped file
    dropzone.addEventListener('drop', handleDrop, false);
    
    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }
    
    function highlight() {
        dropzone.classList.add('active');
    }
    
    function unhighlight() {
        dropzone.classList.remove('active');
    }
    
    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;
        
        if (files.length !== 1) {
            alert('Please drop exactly one spots.json file.');
            return;
        }
        
        const file = files[0];
        
        // Read and process the file
        const reader = new FileReader();
        reader.onload = function(event) {
            try {
                const { spots } = JSON.parse(event.target.result);
                colorSpots(spots);
                dropzone.innerHTML = `Loaded spots.json with ${Object.keys(spots).length} locations!`;
            } catch (error) {
                console.error('Error parsing spots.json:', error);
                alert('Error parsing the file. Make sure it\'s a valid spots.json file with a "spots" property.');
            }
        };
        reader.readAsText(file);
    }
}

document.addEventListener('DOMContentLoaded', async () => {
    // Create a loading indicator
    const loadingIndicator = document.createElement('div');
    loadingIndicator.innerText = 'Loading maps...';
    loadingIndicator.style = 'position: fixed; top: 10px; right: 10px; background: #555; padding: 10px; border-radius: 5px;';
    document.body.appendChild(loadingIndicator);
    
    try {
        // Get the map filenames
        const mapFilenames = await fetchMapList();
        
        // Create instructions
        const instructions = document.createElement('p');
        instructions.innerHTML = `Hover over a map to see the X,Y coordinates. <strong>${mapFilenames.length}</strong> maps available.`;
        document.body.insertBefore(instructions, document.getElementById('dropzone'));
        
        // Initialize dropzone functionality
        setupDragAndDrop();
        
        // Process each map file and keep track of successfully loaded maps
        const loadedMaps = [];
        const loadPromises = [];
        
        for (const mapFilename of mapFilenames) {
            const title = mapFilename.replace('.png', '');
            const mapPath = `assets/maps/${mapFilename}`;
            const loadPromise = initMap(title, mapPath).then(success => {
                if (success) {
                    loadedMaps.push(title);
                }
                return success;
            });
            loadPromises.push(loadPromise);
        }
        
        // Wait for all maps to load, then create navigation
        await Promise.all(loadPromises);
        createNavigation(loadedMaps);
        
        // Update instructions with actual loaded count
        instructions.innerHTML = `Hover over a map to see the X,Y coordinates. <strong>${loadedMaps.length}</strong> maps loaded.`;
        
    } catch (error) {
        console.error("Error loading maps:", error);
        const errorMessage = document.createElement('p');
        errorMessage.innerText = 'Error loading maps: ' + error.message;
        errorMessage.style.color = '#ff5555';
        document.body.appendChild(errorMessage);
    } finally {
        // Remove loading indicator
        loadingIndicator.remove();
    }
});
