// Tester.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <ip_geo_item.h>

int _tmain(int argc, _TCHAR* argv[])
{
	map <pair<unsigned long, unsigned long>, ip_geo_item> geo_map;
	FILE *stream = NULL;
	char line[5*1024];
	long counter = 0;
	vector<string> values(28);
	boost::timer timer;
	errno_t err = fopen_s(&stream, "c:\\1\\Dev\\ISO3166\\fulldata.txt", "r");
	if (NULL != stream)
	{
		timer.restart();
		while (fgets (line, 5*1024, stream) != NULL)
		{
			values.clear();
			algorithm::split(values, line, algorithm::is_any_of("|"));
			if (!values.empty())
			{
				assert(values.size()==25);
				try
				{
					geo_item g_item(wstring(CA2W(values[2].c_str())));
					g_item.m_region=values[3];
					g_item.m_city=values[4];
					g_item.m_country_conf=boost::lexical_cast<int>(values[6]);
					g_item.m_region_conf=boost::lexical_cast<int>(values[7]);
					g_item.m_city_conf=boost::lexical_cast<int>(values[8]);
					g_item.m_metro_code=boost::lexical_cast<int>(values[9]);
					g_item.m_latitude=boost::lexical_cast<double>(values[10]);
					g_item.m_longitude=boost::lexical_cast<double>(values[11]);
					g_item.m_country_code=boost::lexical_cast<int>(values[12]);
					g_item.m_region_code=boost::lexical_cast<int>(values[13]);
					g_item.m_city_code=boost::lexical_cast<int>(values[14]);
					g_item.m_continent_code=boost::lexical_cast<int>(values[15]);
					g_item.m_area_code=boost::lexical_cast<int>(values[17]);
					g_item.m_zip_code=boost::lexical_cast<int>(values[18]);
					g_item.m_gmt_offset=boost::lexical_cast<int>(values[19]);
					g_item.m_in_dst=values[20] != "n";
					g_item.m_zip_code_text=values[21];

					ip_geo_item item(g_item);
					item.set_start_ip(values[0]);
					item.set_end_ip(values[1]);
					//item.m_isp_name = values[23];
					if (values[5] == "broadband")
						item.m_connection_type = ip_geo_item::broadband;
					else if (values[5] == "cable")
						item.m_connection_type = ip_geo_item::cable;
					else if (values[5] == "xdsl")
						item.m_connection_type = ip_geo_item::xdsl;
					else if (values[5] == "dsl")
						item.m_connection_type = ip_geo_item::dsl;
					else if (values[5] == "dialup")
						item.m_connection_type = ip_geo_item::dialup;
					else if (values[5] == "t1")
						item.m_connection_type = ip_geo_item::t1;
					else if (values[5] == "mobile")
						item.m_connection_type = ip_geo_item::mobile;
					else if (values[5] == "wireless")
						item.m_connection_type = ip_geo_item::wireless;
					else if (values[5] == "satellite")
						item.m_connection_type = ip_geo_item::satellite;
					else if (values[5] == "t3")
						item.m_connection_type = ip_geo_item::t3;
					else if (values[5] == "oc3")
						item.m_connection_type = ip_geo_item::oc3;
					else if (values[5] == "oc12")
						item.m_connection_type = ip_geo_item::oc12;
					else
						cout << "Unknown connection type: '" << values[5] << "'" << endl;
					//start_ip|end_ip|country|region|city|conn-speed|country-conf|region-conf|city-conf|metro-code|latitude|longitude|country-code|region-code|city-code|continent-code|two-letter-country|area-code|zip-code|gmt-offset|in-dst|zip-code-text|zip-country|isp-name|
					//1.8.1.0|1.8.1.255|chn|11|beijing|broadband|5|4|4|-1|039.912|0116.389|156|10664|3036|4|cn|0|0|+800|n|100000|chn|knet techonlogy co. ltd.|
					typedef pair<unsigned long, unsigned long> long_pair;
					long_pair ip_pair(item.m_start_ip, item.m_end_ip);
					geo_map.insert(pair<long_pair, ip_geo_item>(ip_pair, item));
					counter++;
					if (counter % 100000 == 0)
						cout << counter << " records parsed" <<endl;
				}
				catch (std::exception &ex)
				{
					int i=0;
				}
			}
		}
	}
	fclose(stream);
	cout << "Loaded " << counter << " records." << endl;
	cout << geo_map.size() << " records according to storage map" << endl;
	cout << "Time to load: " << timer.elapsed() << endl;
	getchar();
	return 0;
}

